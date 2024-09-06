using Framework.Aggregates;

namespace Framework.Snapshotting;

public interface ISnapshotter<TAggregate> where TAggregate : IAggregate
{
    Task Persist(TAggregate aggregate);
    Task<TAggregate> Load(StreamId streamId);
}