using SimpleMigrations;

namespace PaintAGrid.Web.Grid;

[Migration(2, "Create Grid Identity Generator")]
public class CreateGridIdentityGenerator : Migration
{
    protected override void Up()
    {
        Execute(@"
            CREATE SEQUENCE IF NOT EXISTS grid_id_seq
        ");
    }

    protected override void Down()
    {
        Execute(@"
            DROP SEQUENCE IF EXISTS grid_id_seq
        ");
    }
}