using Dapper;
using Npgsql;

namespace Framework;

public class EventStore(NpgsqlConnection connection)
{
    public async Task AppendEvent(object @event, string eventType, Guid streamId, long? expectedVersion)
    {
        await CheckVersion(streamId, expectedVersion);
        var appendEventSql = """
                             DO $$ 
                             BEGIN
                                DECLARE stream_version int;
                                SELECT position INTO stream_version;
                                    FROM event_store
                                    WHERE stream_id = :streamId
                                    ORDER BY position DESC
                                    LIMIT 1;
                                    
                                IF :expected_version IS NOT NULL AND stream_version != :expected_version THEN
                                    RAISE EXCEPTION 'WrongExpectedVersion';
                                    RETURN;
                                END IF;
                                
                                stream_version := stream_version + 1;
                                
                                INSERT INTO event_store 
                                    (stream_id, position, event_type, event_data, metadata)
                             END $$;
                             """;
    }


    private async Task<long?> GetLastPositionOf(Guid streamId)
    {
        await using var cmd = new NpgsqlCommand(@"
            SELECT position FROM event_store
            WHERE stream_id = @streamId
            ORDER BY position DESC
            LIMIT 1
        ", connection);
        cmd.Parameters.AddWithValue("streamId", streamId);
        var position = await cmd.ExecuteScalarAsync();
        return (long?)position;
    }

    private async Task CheckVersion(Guid streamId, long? expectedVersion)
    {
        var lastPositionInStream = await GetLastPositionOf(streamId);
        if (lastPositionInStream == null)
        {
            if (expectedVersion != null && expectedVersion != 0)
            {
                throw new Exception("Stream not found");
            }
        }
        else
        {
            if (expectedVersion != null && lastPositionInStream != expectedVersion)
            {
                throw new Exception(
                    $"Version conflict, expected version {expectedVersion} but found {lastPositionInStream}");
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