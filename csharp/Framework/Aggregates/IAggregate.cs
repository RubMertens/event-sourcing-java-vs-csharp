namespace Framework;

public interface IAggregate
{
    Guid StreamId { get; }
    int Version { get; }

    IEnumerable<object> DequeueUncommittedEvents();
}