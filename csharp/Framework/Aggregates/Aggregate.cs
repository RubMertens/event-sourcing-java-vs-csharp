namespace Framework;

public abstract class Aggregate : IAggregate
{
    public Guid StreamId { get; protected set; }
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