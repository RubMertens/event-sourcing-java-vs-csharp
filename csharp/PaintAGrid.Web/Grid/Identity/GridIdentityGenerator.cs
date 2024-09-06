using Framework.SqlConnection;

namespace PaintAGrid.Web.Grid.Identity;

public class GridIdentityGenerator(IDbConnectionFactory connectionFactory)
{
    public Task<int> GetNext()
    {
    }
}