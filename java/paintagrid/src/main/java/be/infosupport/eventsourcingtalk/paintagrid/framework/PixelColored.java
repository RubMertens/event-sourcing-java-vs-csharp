package be.infosupport.eventsourcingtalk.paintagrid.framework;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class PixelColored {
    private final int x;
    private final int y;
    private final String color;
}
