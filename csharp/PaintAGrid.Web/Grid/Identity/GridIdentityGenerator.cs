using Dapper;
using Framework.SqlConnection;

namespace PaintAGrid.Web.Grid.Identity;

public class GridIdentityGenerator(IDbConnectionFactory connectionFactory)
{
    public async Task<int> GetNext()
    {
        await using var connection = connectionFactory.GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"SELECT nextval('grid_id_seq')");
    }
}