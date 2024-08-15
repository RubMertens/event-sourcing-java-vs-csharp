namespace Framework;

public interface IEventTypeRegistry
{
    Type GetTypeByName(string eventType);
    string GetNameByType(Type eventType);
}