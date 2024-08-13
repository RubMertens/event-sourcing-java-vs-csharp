using Npgsql;

namespace Framework.Tests;

public class EventStoreTests
{


    
    [Test]
    public async Task ShouldPersistEvent()
    {
        var connectionString =
            "Server=localhost;Port=5432;Userid=sa;Password=password;Database=testing";
        
        var conn = new NpgsqlConnection(connectionString);
    }
}