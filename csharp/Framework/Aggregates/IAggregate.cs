namespace Framework.Aggregates;

public interface IAggregate
{
    StreamId StreamId { get; }
    int Version { get; }

    IEnumerable<object> DequeueUncommittedEvents();
}