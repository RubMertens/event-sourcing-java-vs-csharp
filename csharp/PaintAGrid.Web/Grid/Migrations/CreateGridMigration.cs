using SimpleMigrations;

namespace PaintAGrid.Web.Grid.Migrations;

[Migration(1, "Create Grid Table")]
public class CreateGridMigration : Migration
{
    protected override void Up()
    {
        Execute("""
                    CREATE TABLE IF NOT EXISTS grid (
                        stream_id UUID PRIMARY KEY,
                        name TEXT NOT NULL,
                        width INT NOT NULL,
                        height INT NOT NULL
                    )
                """);
        Execute("""
                CREATE TABLE IF NOT EXISTS grid_cells (
                    grid_id UUID NOT NULL,
                    x INT NOT NULL,
                    y INT NOT NULL,
                    color TEXT NOT NULL,
                    PRIMARY KEY (grid_id, x, y)
                )
                """);
    }

    protected override void Down()
    {
        Execute("DROP TABLE IF EXISTS grid_cells");
        Execute("DROP TABLE IF EXISTS grid");
    }
}