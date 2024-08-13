using Dapper;
using Npgsql;

namespace Framework;



public class EventStore(NpgsqlConnection connection)
{
    public async Task AppendEvent(object @event, Guid streamId, long? expectedVersion)
    {
        
    }
    
    private async Task GetLastPositionOf(Guid streamId)
    {
        await using var cmd = new NpgsqlCommand(@"
            SELECT position FROM event_store
            WHERE stream_id = @streamId
            ORDER BY position DESC
            LIMIT 1
        ", connection);
        cmd.Parameters.AddWithValue("streamId", streamId);
        return await cmd.ExecuteReaderAsync();
    }
    
    private async Task CheckVersion( Guid streamId, long? expectedVersion)
    {
        var lastEvent = await GetLastPositionOf(streamId);
        if (lastEvent == null)
        {
            if (expectedVersion != null && expectedVersion != 0)
            {
                throw new Exception("Stream not found");
            }
        }
        else
        {
            if (expectedVersion != null && lastEvent.Position != expectedVersion)
            {
                throw new Exception("Version conflict");
            }
        }
    }
    
    private async Task CreateEventStoreTable()
    {
        await using var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS event_store (
                id SERIAL PRIMARY KEY,
                stream_id UUID NOT NULL,
                position BIGINT NOT NULL,
                event_type TEXT NOT NULL,
                event_data JSONB NOT NULL,
                metadata JSONB NOT NULL,
                created_at TIMESTAMPTZ NOT NULL default now(),
                unique(stream_id, position),
            );
        ", connection);
        await cmd.ExecuteNonQueryAsync();
    }
}