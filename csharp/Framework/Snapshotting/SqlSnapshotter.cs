using System.Data;
using Dapper;
using Framework.Aggregates;

namespace Framework.Snapshotting;

public class SqlSnapshotter<TAggregate>(
        IDbConnection connection,
        string updateStatement,
        string loadStatement
    ) : ISnapshotter<TAggregate>
    where TAggregate : IAggregate
{
    public async Task Persist(TAggregate aggregate)
    {
        await connection.ExecuteAsync(updateStatement, aggregate);
    }

    public Task<TAggregate> Load(StreamId streamId)
    {
        throw new NotImplementedException();
    }

    public async Task<TAggregate> Load(Guid streamId)
    {
        return await connection.QuerySingleAsync<TAggregate>(loadStatement,
            new { streamId });
    }
}