using Framework.SqlConnection;

namespace PaintAGrid.Web.Grid.Identity;

public class GridIdentityGenerator(IDbConnectionFactory connectionFactory)
{
    public async Task<int> GetNext()
    {
        await using var connection = connectionFactory.GetConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT nextval('grid_id_seq')
        ";
        return (int)await command.ExecuteScalarAsync();
    }
}