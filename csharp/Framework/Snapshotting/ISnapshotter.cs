namespace Framework;

public interface ISnapshotter<TAggregate> where TAggregate : IAggregate
{
    Task Persist(TAggregate aggregate);
    Task<TAggregate> Load(Guid streamId);
}