using Framework;

namespace PaintAGrid.Web.Grid;

public class GridAggregate : Aggregate
{
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public GridAggregate(Guid streamId, string name, int width, int height)
    {
        var gridCreated = new GridCreated(streamId, name, width, height);
        EnqueueEvent(gridCreated);
        Apply(gridCreated);
    }

    public void Apply(GridCreated evt)
    {
        Name = evt.Name;
        Width = evt.Width;
        Height = evt.Height;
        StreamId = evt.Id;
    }
}

public class GridCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }
}