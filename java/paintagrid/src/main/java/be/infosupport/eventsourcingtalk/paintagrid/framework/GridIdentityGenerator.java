package be.infosupport.eventsourcingtalk.paintagrid.framework;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import javax.sql.DataSource;

public class GridIdentityGenerator {
    private final DataSource dataSource;

    public GridIdentityGenerator(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    public int getNext() throws SQLException {
        String query = "SELECT nextval('grid_id_seq')";
        try (Connection connection = dataSource.getConnection();
             PreparedStatement statement = connection.prepareStatement(query);
             ResultSet resultSet = statement.executeQuery()) {
            if (resultSet.next()) {
                return resultSet.getInt(1);
            } else {
                throw new SQLException("Failed to retrieve next grid ID");
            }
        }
    }
}
