package be.infosupport.eventsourcingtalk.paintagrid.controller;

import be.infosupport.eventsourcingtalk.paintagrid.framework.EventStore;
import be.infosupport.eventsourcingtalk.paintagrid.framework.GridIdentityGenerator;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/grids")
class GridController {

    @Autowired
    private EventStore eventStore;

    @Autowired
    private GridIdentityGenerator identityGenerator;

    @GetMapping("/{id}")
    public GridAggregate getGrid(@PathVariable int id) {
        return eventStore.aggregateStreamFromSnapshot(GridAggregate.streamIdFromId(id));
    }

    @GetMapping
    public String getAllGrids() {
        throw new UnsupportedOperationException();
    }

    @PostMapping
    public GridAggregate createGrid(@RequestBody CreateGrid createGrid) throws Exception {
        GridAggregate grid = new GridAggregate(
                identityGenerator.getNext(),
                createGrid.getName(),
                createGrid.getWidth(),
                createGrid.getHeight()
        );
        eventStore.store(grid);
        return grid;
    }

    @PostMapping("/{id}/color")
    public GridAggregate colorPixel(@PathVariable int id, @RequestBody ColorPixel pixel) throws Exception {
        GridAggregate grid = eventStore.aggregateStreamFromSnapshot(GridAggregate.streamIdFromId(id));
        grid.colorPixel(pixel.getX(), pixel.getY(), pixel.getColor());
        eventStore.store(grid);
        return grid;
    }

    @PostMapping("/{id}/move")
    public GridAggregate movePixel(@PathVariable int id, @RequestBody MovePixel move) throws Exception {
        GridAggregate grid = eventStore.aggregateStreamFromSnapshot(GridAggregate.streamIdFromId(id));
        grid.movePixel(move.getX(), move.getY(), move.getDeltaX(), move.getDeltaY());
        eventStore.store(grid);
        return grid;
    }
}
