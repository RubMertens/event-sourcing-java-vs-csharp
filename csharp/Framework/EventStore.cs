using System.Text.Json;
using Dapper;
using Npgsql;

namespace Framework;

public class EventStore(
    NpgsqlConnection connection,
    IEventTypeRegistry eventTypeRegistry
)
{
    private Dictionary<Type, object> snapshotHandlers = new();

    public async Task Init()
    {
        await CreateEventStoreTable();
        await CreateAppendEventFunction();
    }

    public void RegisterSnapshot<TAggregate>(
        ISnapshotter<TAggregate> snapshotter)
        where TAggregate : IAggregate
    {
        snapshotHandlers[typeof(TAggregate)] = snapshotter;
    }

    public async Task Store<TAggregate>(TAggregate aggregate)
        where TAggregate : IAggregate
    {
        var events = aggregate.DequeueUncommittedEvents().ToList();
        var originalVersion = aggregate.Version - events.Count;
        foreach (var @event in events)
        {
            await AppendEvent(@event, aggregate.StreamId, originalVersion++);
        }

        var snapshotHandler =
            snapshotHandlers.GetValueOrDefault(aggregate.GetType());
        if (snapshotHandler != null)
        {
            await ((ISnapshotter<TAggregate>)snapshotHandler)
                .Persist(aggregate);
        }
    }

    public async Task<TAggregate> AggregateStreamFromSnapshot<TAggregate>(Guid streamId)
    where TAggregate: IAggregate
    {
        var snapshotHandler =
            snapshotHandlers.GetValueOrDefault(typeof(TAggregate));
        TAggregate aggregate = default;
        if (snapshotHandler != null)
            
        {
            aggregate =  await ((ISnapshotter<TAggregate>)snapshotHandler)
                .Load(streamId);
        }
        aggregate ??= Activator.CreateInstance<TAggregate>();
        var events = await GetEventFrom(streamId, aggregate.Version);
        foreach (var @event in events)
        {
            aggregate.InvokeApplyMethod(@event);
        }
        return aggregate;
    }
    
    public async Task<TAggregate> AggregateStream<TAggregate>(Guid streamId,
        int? untilVerion = null)
    {
        

        var events = untilVerion.HasValue
            ? await GetEventsUntil(streamId, untilVerion.Value)
            : await GetEvents(streamId);

        var aggregate =
            (TAggregate)Activator.CreateInstance(typeof(TAggregate));

        if (aggregate == null)
        {
            throw new Exception(
                $"Could not create instance of {typeof(TAggregate).Name}");
        }

        foreach (var @event in events)
        {
            aggregate.InvokeApplyMethod(@event);
        }

        return aggregate;
    }

    public async Task<IEnumerable<object>> GetEventFrom(Guid streamId,
        int fromVersion)
    {
        var results = await connection.QueryAsync<dynamic>(
            """
                SELECT event_data, event_type
                FROM event_store 
                WHERE stream_id = :p_stream_id AND position >= :p_from_version
                ORDER BY position
            """,
            new { p_stream_id = streamId, p_from_version = fromVersion });

        return results
            .Select(result =>
                JsonSerializer.Deserialize(result.event_data,
                    eventTypeRegistry.GetTypeByName(result.event_type))
            );
    }

    public async Task<IEnumerable<object>> GetEventsUntil(Guid streamId,
        int untilVersion)
    {
        var results = await connection.QueryAsync<dynamic>(
            """
                SELECT event_data, event_type
                FROM event_store 
                WHERE stream_id = :p_stream_id AND position <= :p_until_version
                ORDER BY position
            """,
            new { p_stream_id = streamId, p_until_version = untilVersion });

        return results
            .Select(result =>
                JsonSerializer.Deserialize(result.event_data,
                    eventTypeRegistry.GetTypeByName(result.event_type))
            );
    }

    public async Task<IEnumerable<object>> GetEvents(Guid streamId)
    {
        var results = await connection.QueryAsync<dynamic>(
            """
                SELECT event_data, event_type
                FROM event_store 
                WHERE stream_id = :p_stream_id 
                ORDER BY position
            """,
            new { p_stream_id = streamId });

        return results
            .Select(result =>
                JsonSerializer.Deserialize(result.event_data,
                    eventTypeRegistry.GetTypeByName(result.event_type))
            );
    }

    public async Task AppendEvent(object @event, Guid streamId,
        long? expectedVersion)
    {
        try
        {
            await connection.QuerySingleAsync(
                "SELECT append_event(@p_event::jsonb, @p_event_type, @p_stream_id, @p_expected_version)",
                new
                {
                    p_event = JsonSerializer.Serialize(@event),
                    p_event_type =
                        eventTypeRegistry.GetNameByType(@event.GetType()),
                    p_stream_id = streamId,
                    p_expected_version = expectedVersion ?? 0
                });
        }
        catch (PostgresException e) when (e.SqlState == "P0001")
        {
            throw new ConcurrencyException(e.Message);
        }
    }

    private async Task CreateEventStoreTable()
    {
        await connection.ExecuteAsync("""
                                      CREATE TABLE IF NOT EXISTS event_store (
                                          id SERIAL PRIMARY KEY,
                                          stream_id UUID NOT NULL,
                                          position BIGINT NOT NULL,
                                          event_type TEXT NOT NULL,
                                          event_data JSONB NOT NULL,
                                          metadata JSONB NOT NULL,
                                          created_at TIMESTAMPTZ NOT NULL default now(),
                                          unique(stream_id, position)
                                      ) 
                                      """);
    }

    private async Task CreateAppendEventFunction()
    {
        await connection.ExecuteAsync("""
                                        CREATE OR REPLACE FUNCTION append_event(
                                                                  p_event JSONB,
                                                                  p_event_type TEXT,
                                                                  p_stream_id UUID,
                                                                  p_expected_version BIGINT
                                                              ) RETURNS VOID AS $$
                                                              DECLARE
                                                                  latest_version BIGINT;
                                                              BEGIN
                                      
                                                                  -- Get the latest version for the given stream_id
                                                                  SELECT COALESCE(MAX(position), -1) INTO latest_version
                                                                  FROM event_store
                                                                  WHERE stream_id = p_stream_id;
                                                                  
                                                                  -- Check if the expected version matches the latest version
                                                                  IF p_expected_version = 0 AND latest_version != -1 THEN
                                                                      RAISE EXCEPTION 'Expected version is 0 but stream already has events';
                                                                  ELSIF p_expected_version > 0 AND latest_version+1 != p_expected_version THEN
                                                                      RAISE EXCEPTION 'Expected version % does not match the latest version %',
                                                                                      p_expected_version, latest_version;
                                                                  END IF;
                                                                  
                                                                  -- Insert the new event with the next position
                                                                  INSERT INTO event_store (stream_id, position, event_type, event_data, metadata)
                                                                  VALUES (p_stream_id, latest_version + 1, p_event_type, p_event, '{}');
                                                              END;
                                                              $$ LANGUAGE plpgsql;
                                      """);
    }
}