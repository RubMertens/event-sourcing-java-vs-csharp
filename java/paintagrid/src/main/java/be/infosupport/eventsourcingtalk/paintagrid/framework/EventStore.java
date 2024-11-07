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
