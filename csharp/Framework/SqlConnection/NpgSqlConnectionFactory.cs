using System.Data.Common;
using Npgsql;

namespace Framework.SqlConnection;

public interface IDbConnectionFactory
{
    DbConnection GetConnection();
}

public class NpgSqlConnectionFactory(string connectionString)
    : IDisposable, IAsyncDisposable, IDbConnectionFactory
{
    private NpgsqlConnection _connection = new(connectionString);

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}