using Framework;
using Framework.Aggregates;

namespace PaintAGrid.Web.Grid;

public class GridAggregate : Aggregate
{
    public const string StreamName = "Grid";
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public GridCell[][] Cells { get; set; }

    public GridAggregate(long id, string name, int width, int height)
    {
        var gridCreated = new GridCreated(id, name, width, height);
        EnqueueEvent(gridCreated);
        Apply(gridCreated);
    }

    public GridAggregate()
    {
    }

    public void ColorPixel(int x, int y, string color)
    {
        var pixelColored = new PixelColored(x, y, color);
        EnqueueEvent(pixelColored);
        Apply(pixelColored);
    }

    public void MovePixel(int x, int y, int newX, int newY)
    {
        var pixelMoved = new PixelMoved(x, y, newX, newY);
        EnqueueEvent(pixelMoved);
        Apply(pixelMoved);
    }

    public void Apply(GridCreated evt)
    {
        Name = evt.Name;
        Width = evt.Width;
        Height = evt.Height;
        StreamId = StreamIdFromId(evt.Id);
        Cells = new GridCell[Width][];
    }

    public void Apply(PixelColored evt)
    {
        if (Cells[evt.X] == null)
        {
            Cells[evt.X] = new GridCell[Height];
        }

        Cells[evt.X][evt.Y] = new GridCell
        {
            X = evt.X,
            Y = evt.Y,
            Color = evt.Color
        };
    }

    public void Apply(PixelMoved evt)
    {
        var cell = Cells[evt.X][evt.Y];
        Cells[evt.X][evt.Y] = null;
        Cells[evt.NewX][evt.NewY] = cell;
    }

    public static StreamId StreamIdFromId(long id) =>
        new StreamId(GridAggregate.StreamName, id.ToString());
}

public class GridCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }
}