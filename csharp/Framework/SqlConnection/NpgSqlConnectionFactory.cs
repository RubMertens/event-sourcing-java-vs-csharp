using System.Data.Common;
using Npgsql;

namespace Framework.SqlConnection;

public interface IDbConnectionFactory
{
    DbConnection GetConnection();
}

public class NpgSqlConnectionFactory(string connectionString)
    : IDbConnectionFactory
{
    public DbConnection GetConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}