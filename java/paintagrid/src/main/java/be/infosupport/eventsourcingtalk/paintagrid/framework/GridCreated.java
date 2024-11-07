package be.infosupport.eventsourcingtalk.paintagrid.framework;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class GridCreated {
    private final long id;
    private final String name;
    private final int width;
    private final int height;
}
