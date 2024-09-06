namespace PaintAGrid.Web.Grid;

public record GridCreated(int Id, string Name, int Width, int Height);

public record PixelColored(int X, int Y, string Color);

public record PixelMoved(int X, int Y, int NewX, int NewY);