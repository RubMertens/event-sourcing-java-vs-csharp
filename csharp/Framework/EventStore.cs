using System.Text.Json;
using Dapper;
using Npgsql;

namespace Framework;

public class EventStore(NpgsqlConnection connection)
{
    public async Task Init()
    {
        await CreateEventStoreTable();
        await CreateAppendEventFunction();
    }

    public async Task AppendEvent(object @event, string eventType, Guid streamId, long? expectedVersion)
    {
        await connection.QuerySingleAsync(
            "SELECT append_event(@p_event, @p_event_type, @p_stream_id, @p_expected_version)",
            new
            {
                p_event = JsonSerializer.Serialize(@event),
                p_event_type = eventType,
                p_stream_id = streamId,
                p_expected_version = expectedVersion
            });
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

    private async Task CreateAppendEventFunction()
    {
        await using var cmd = new NpgsqlCommand(
            """
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
                                        ELSIF p_expected_version > 0 AND latest_version != p_expected_version THEN
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