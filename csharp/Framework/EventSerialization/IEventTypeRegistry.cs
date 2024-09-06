namespace Framework.EventSerialization;

public interface IEventTypeRegistry
{
    Type GetTypeByName(string eventType);
    string GetNameByType(Type eventType);
}