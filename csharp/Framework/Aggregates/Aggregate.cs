namespace Framework.Aggregates;

public abstract class Aggregate : IAggregate
{
    public StreamId StreamId { get; protected set; }
    public int Version { get; protected set; } = 0;

    private readonly List<object> _uncommittedEvents = new();

    public IEnumerable<object> DequeueUncommittedEvents()
    {
        var events = _uncommittedEvents.ToList();
        _uncommittedEvents.Clear();
        return events;
    }

    protected void EnqueueEvent(object @event)
    {
        Version++;
        _uncommittedEvents.Add(@event);
    }
}