using Framework.EventSerialization;

namespace PaintAGrid.Web.Grid;

[EventType("grid-created")]
public record GridCreated(long Id, string Name, int Width, int Height);

[EventType("pixel-colored")]
public record PixelColored(int X, int Y, string Color);

[EventType("pixel-moved")]
public record PixelMoved(int X, int Y, int NewX, int NewY);