using Dapper;
using Npgsql;

namespace Framework;

public class
    SqlSnapshotter<TAggregate>(
        NpgsqlConnection connection,
        string UpdateStatement,
        string LoadStatement
    ) : ISnapshotter<TAggregate>
    where TAggregate : IAggregate
{
    public async Task Persist(TAggregate aggregate)
    {
        await connection.ExecuteAsync(UpdateStatement, aggregate);
    }

    public async Task<TAggregate> Load(Guid streamId)
    {
        return await connection.QuerySingleAsync<TAggregate>(LoadStatement, new {streamId});
    }
}