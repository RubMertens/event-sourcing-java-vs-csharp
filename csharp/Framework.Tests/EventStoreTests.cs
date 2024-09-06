using System.Collections.Specialized;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using Dapper;
using FluentAssertions;
using Framework.Aggregates;
using Framework.EventSerialization;
using Framework.Exceptions;
using Framework.Snapshotting;
using Framework.SqlConnection;
using Npgsql;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;
using Testcontainers.PostgreSql;

namespace Framework.Tests;

public record UserCreated(StreamId Id, string Name);

public record PhoneNumberAssigned(string PhoneNumber);

public class UserAggregate : Aggregate
{
    public const string StreamName = "USER";
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public UserAggregate(int id, string name)
    {
        var userCreated =
            new UserCreated(new StreamId(StreamName, id.ToString()), name);
        EnqueueEvent(userCreated);
        Apply(userCreated);
    }

    public UserAggregate()
    {
    }

    public void ChangePhoneNumber(string phoneNumber)
    {
        var phoneNumberAssigned = new PhoneNumberAssigned(phoneNumber);
        EnqueueEvent(phoneNumberAssigned);
        Apply(phoneNumberAssigned);
    }

    public void Apply(UserCreated created)
    {
        StreamId = created.Id;
        Name = created.Name;
    }

    public void Apply(PhoneNumberAssigned assigned)
    {
        PhoneNumber = assigned.PhoneNumber;
    }
}

[Migration(1, "Create user snapshot table")]
public class UserSnapshotTable : Migration
{
    protected override void Up()
    {
        Execute(@"
                CREATE TABLE IF NOT EXISTS ""user"" (
                    ""streamid"" UUID NOT NULL PRIMARY KEY,
                    ""version"" INT NOT NULL,
                    ""name"" TEXT NOT NULL,
                    ""phone_number"" TEXT
                )
            ");
    }

    protected override void Down()
    {
        Execute(@"DROP TABLE IF EXISTS ""user""");
    }
}

public class EventStoreTests
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:14-alpine")
        .WithCleanUp(true)
        .Build();

    private static readonly StreamId JohnDoeStreamId =
        new StreamId("USER", "1");

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _postgres.StartAsync();
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        await using var connection = factory.GetConnection();
        var migrator = new SimpleMigrator(typeof(EventStoreTests).Assembly,
            new PostgresqlDatabaseProvider(connection));
        migrator.Load();
        migrator.MigrateToLatest();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.DisposeAsync();
    }

    [SetUp]
    public async Task Setup()
    {
    }

    [TearDown]
    public async Task TearDown()
    {
        await _postgres.ExecScriptAsync("DROP TABLE IF EXISTS event_store");
        await _postgres.ExecScriptAsync("TRUNCATE TABLE user");
    }


    [Test]
    public async Task ShouldPersistEvent()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var eventStore =
            new EventStore(
                factory,
                registry);

        await eventStore.Init();
        var @event = new UserCreated(JohnDoeStreamId, "JohnDoe");
        var streamId = new StreamId("USER", "1");
        await eventStore.AppendEvent(@event, streamId, null);
    }

    [Test]
    public async Task WhenStreamNotExists_AndVersionNull_PersistsEvent()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store = new EventStore(
            factory,
            registry);
        await store.Init();

        var evt = new UserCreated(JohnDoeStreamId, "JohnDoe");
        Assert.DoesNotThrowAsync(async () =>
        {
            await store.AppendEvent(evt, JohnDoeStreamId, null);
        });
    }

    [Test]
    public async Task
        WhenStreamExists_AndVersionNull_ThrowsConcurrencyException()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store = new EventStore(
            factory,
            registry);
        await store.Init();

        var evt = new UserCreated(JohnDoeStreamId, "JohnDoe");
        await store.AppendEvent(evt, JohnDoeStreamId, null); // first event

        Assert.ThrowsAsync<ConcurrencyException>(async () =>
        {
            await store.AppendEvent(evt, JohnDoeStreamId,
                null); //retry should fail if event is persisted
        });
    }

    [Test]
    public async Task WhenStreamExists_AndVersionIsSequential_PersistsEvent()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store = new EventStore(
            factory,
            registry);
        await store.Init();

        var evt = new UserCreated(JohnDoeStreamId, "JohnDoe");
        var secondEvent =
            new UserCreated(new StreamId("USER", "2"),
                "JaneDoe");

        await store.AppendEvent(evt, JohnDoeStreamId,
            null); //creates event at version 1

        Assert.DoesNotThrowAsync(async () =>
        {
            await store.AppendEvent(secondEvent, JohnDoeStreamId,
                1); //second event expects version 1 to exist
        });
    }

    [Test]
    public async Task
        WhenStreamExissts_AndVersionIsLargerThanLastVersionPlusOne_ThrowsConcurrencyException()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store = new EventStore(
            factory,
            registry);
        await store.Init();

        var evt = new UserCreated(JohnDoeStreamId, "JohnDoe");
        var secondEvent = new PhoneNumberAssigned("12345678");

        await store.AppendEvent(evt, JohnDoeStreamId,
            null); //creates event at version 0

        Assert.ThrowsAsync<ConcurrencyException>(async () =>
        {
            await store.AppendEvent(secondEvent, JohnDoeStreamId,
                2); //second event
        });
    }

    [Test]
    public async Task
        WhenStreamExists_AndVersionIsSmallerThanLastVersion_ThrowsConcurrencyException()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());

        var store = new EventStore(
            factory,
            registry);
        await store.Init();

        var evt = new UserCreated(JohnDoeStreamId, "JohnDoe");
        var secondEvent = new PhoneNumberAssigned("123");
        var thirdEvent = new PhoneNumberAssigned("456");
        var fourthEvent = new PhoneNumberAssigned("789");

        await store.AppendEvent(evt, JohnDoeStreamId,
            null); //creates event at version 0
        await store.AppendEvent(secondEvent, JohnDoeStreamId, 1); //creates v2
        await store.AppendEvent(thirdEvent, JohnDoeStreamId, 2); // creates V3


        Assert.ThrowsAsync<ConcurrencyException>(async () =>
        {
            await store.AppendEvent(fourthEvent, JohnDoeStreamId,
                1);
        });
    }


    [Test]
    public async Task ShouldReturnEventsOfStream()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store =
            new EventStore(
                factory,
                registry);
        await store.Init();

        object[] expectedEvents =
        [
            new UserCreated(JohnDoeStreamId, "JohnDoe"),
            new PhoneNumberAssigned("123456789")
        ];

        for (var i = 0; i < expectedEvents.Length; i++)
        {
            var evt = expectedEvents[i];
            await store.AppendEvent(evt, JohnDoeStreamId, i);
        }


        var otherStreamId = new StreamId(UserAggregate.StreamName, "3");
        await store.AppendEvent(
            new UserCreated(new StreamId(UserAggregate.StreamName, "2"),
                "JaneDoe"),
            otherStreamId, null);


        var result = await store.GetEvents(JohnDoeStreamId);

        result.Should().BeEquivalentTo(expectedEvents);
    }

    [Test]
    public async Task AbleToAggregateIntoStream()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());

        var store =
            new EventStore(
                factory,
                registry);
        await store.Init();
        var streamId = new StreamId("USER", "1");
        object[] events =
        [
            new UserCreated(JohnDoeStreamId, "JohnDoe"),
            new PhoneNumberAssigned("123456789")
        ];
        for (var i = 0; i < events.Length; i++)
        {
            var evt = events[i];
            await store.AppendEvent(evt, streamId, i);
        }

        var aggregate = await store.AggregateStream<UserAggregate>(streamId);

        aggregate.Name.Should().Be("JohnDoe");
        aggregate.PhoneNumber.Should().Be("123456789");
    }

    [Test]
    public async Task ShouldAggregateToSpecificVerion()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store =
            new EventStore(
                factory,
                registry);
        await store.Init();
        var streamId = new StreamId(UserAggregate.StreamName, "1");
        object[] events =
        [
            new UserCreated(JohnDoeStreamId, "JohnDoe"),
            new PhoneNumberAssigned("123456789"),
            new PhoneNumberAssigned("987654321")
        ];
        for (var i = 0; i < events.Length; i++)
        {
            var evt = events[i];
            await store.AppendEvent(evt, streamId, i);
        }

        var atVersion0 =
            await store.AggregateStream<UserAggregate>(streamId, 0);
        var atVersion1 =
            await store.AggregateStream<UserAggregate>(streamId, 1);

        atVersion0.Name.Should().Be("JohnDoe");
        atVersion0.PhoneNumber.Should().BeNull();

        atVersion1.Name.Should().Be("JohnDoe");
        atVersion1.PhoneNumber.Should().Be("123456789");
    }

    [Test]
    public async Task Snapshot()
    {
        var registry = new EventTypeRegistry();
        registry.Register<UserCreated>(nameof(UserCreated));
        registry.Register<PhoneNumberAssigned>(nameof(PhoneNumberAssigned));
        var factory =
            new NpgSqlConnectionFactory(_postgres.GetConnectionString());
        var store =
            new EventStore(
                factory,
                registry);
        await store.Init();

        //create snapshot table
        await using var connection =
            factory.GetConnection();

        var sqlSnapshotter = new SqlSnapshotter<UserAggregate>(
            connection,
            """
            INSERT INTO "user" (streamid, version, name, phone_number) VALUES (@streamid, @version, @name, @phoneNumber)
            ON CONFLICT (streamid) DO UPDATE SET version = @version, name = @name, phone_number = @phoneNumber
            """,
            """
            SELECT streamid, version, name, phone_number as phoneNumber FROM "user" WHERE streamid = @streamid
            """
        );

        store.RegisterSnapshot(sqlSnapshotter);

        var user = new UserAggregate(JohnDoeStreamId, "JohnDoe");
        user.ChangePhoneNumber("123456789");
        await store.Store(user);

        var aggregate =
            await store.AggregateStreamFromSnapshot<UserAggregate>(
                JohnDoeStreamId);
        aggregate.Name.Should().Be("JohnDoe");
        aggregate.PhoneNumber.Should().Be("123456789");
        aggregate.Version.Should().Be(2);
        aggregate.StreamId.Should().Be(JohnDoeStreamId);

        await store.AppendEvent(new PhoneNumberAssigned("987654321"),
            JohnDoeStreamId,
            aggregate.Version
        );

        aggregate =
            await store.AggregateStreamFromSnapshot<UserAggregate>(
                JohnDoeStreamId);

        aggregate.Name.Should().Be("JohnDoe");
        aggregate.PhoneNumber.Should().Be("987654321");
    }
}