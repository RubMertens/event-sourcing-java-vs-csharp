package be.infosupport.eventsourcingtalk.paintagrid.framework;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.Optional;
import java.util.UUID;

public class EventStore {
    private final Connection dbConnection;

    public EventStore(Connection dbConnection) {
        this.dbConnection = dbConnection;
        String createTableSql = """
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
        """;
        String createFunctionAppendEventSql = """
                        CREATE OR REPLACE FUNCTION append_event(
                            p_event JSONB,
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
                            VALUES (p_stream_id, latest_version + 1, p_event->>'event_type', p_event, '{}');
                        END;
                        $$ LANGUAGE plpgsql;
                """;
        exectuteSql(dbConnection, createTableSql);
        exectuteSql(dbConnection, createFunctionAppendEventSql);
    }

    public void AppendEvent(Object event, UUID streamId, Optional<Long> expectedVersion){
        String appendEventSql = """
            
    """;
    }


    public static void exectuteSql(Connection dbConnection, String sql) {
        try (var statement = dbConnection.createStatement()) {
            statement.execute(sql);
        } catch (SQLException e) {
            throw new RuntimeException(e);
        }
    }
}
