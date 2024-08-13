package be.infosupport.eventsourcingtalk.paintagrid.framework;

import org.springframework.stereotype.Service;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.Optional;
import java.util.UUID;

import static be.infosupport.eventsourcingtalk.paintagrid.framework.JsonEventSerializer.serialize;

@Service
public class EventStore {
    private final Connection dbConnection;

    public EventStore(Connection dbConnection) {
        this.dbConnection = dbConnection;
    }

    public void init() {
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
                            p_expected_version BIGINT,
                            p_event_type TEXT
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
                """;
        exectuteSql(dbConnection, createTableSql);
        exectuteSql(dbConnection, createFunctionAppendEventSql);
    }

    public void AppendEvent(Object event, String eventType, UUID streamId, Optional<Long> expectedVersion) {
        // Convert event to JSON (assuming you have a method to do this)
        String eventJson = convertEventToJson(event); // You need to implement this

        // Prepare the SQL statement to call the append_event function
        String appendEventSql = """
                SELECT append_event(
                '%s'::jsonb,
                '%s'::uuid,
                %d,
                '%s'::text);""".formatted(eventJson, streamId.toString(), expectedVersion.orElse(0L), eventType);

        // Execute the SQL
        exectuteSql(dbConnection, appendEventSql);
    }

    private String convertEventToJson(Object event) {
        return serialize(event);
    }


    public static void exectuteSql(Connection dbConnection, String sql) {
        try (var statement = dbConnection.createStatement()) {
            statement.execute(sql);
        } catch (SQLException e) {
            throw new RuntimeException(e);
        }
    }
}
