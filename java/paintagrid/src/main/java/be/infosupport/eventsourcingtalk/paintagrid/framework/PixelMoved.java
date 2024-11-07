package be.infosupport.eventsourcingtalk.paintagrid.framework;

import lombok.Data;
import lombok.AllArgsConstructor;

@Data
@AllArgsConstructor
public class PixelMoved {
    private final int x;
    private final int y;
    private final int newX;
    private final int newY;
}
