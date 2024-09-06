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
        return connection.QuerySingleAsync<TAggregate>(loadStatement,
            new { streamId = streamId.ToString() });
    }

    static SqlSnapshotter()
    {
        SqlMapper.AddTypeHandler(new StreamIdTypeHandler());
    }
}
public class StreamIdTypeHandler : SqlMapper.TypeHandler<StreamId>
{
    public override StreamId Parse(object value)
    {
        var str = (string)value;
        var parts = str.Split('-');
        return new StreamId(parts[0], parts[1]);
    }

    public override void SetValue(IDbDataParameter parameter, StreamId value)
    {
        parameter.Value = value.ToString();
    }
}