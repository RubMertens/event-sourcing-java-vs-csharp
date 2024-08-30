using Npgsql;
using PaintAGrid.Web.SqlConnection;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;

namespace PaintAGrid.Web;

public class MigrationExecution(IDbConnectionFactory connectionFactory)
{
    public void Migrate()
    {
        var connection = connectionFactory.GetConnection();
        var databaseProvider = new PostgresqlDatabaseProvider(connection);
        var migrator = new SimpleMigrator(typeof(MigrationExecution).Assembly,
            databaseProvider);
        migrator.Load();
        migrator.MigrateToLatest();
    }
}