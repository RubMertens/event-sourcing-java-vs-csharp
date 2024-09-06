namespace Framework;

public record StreamId(string NameSpace, string Id)
{
    public override string ToString()
    {
        return NameSpace + "-" + Id;
    }
};