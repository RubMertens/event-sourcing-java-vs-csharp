namespace Framework;

public class EventTypeRegistry : IEventTypeRegistrar, IEventTypeRegistry
{
    private Dictionary<string, Type> _registryByName = new();
    private Dictionary<Type, string> _registryByType = new();

    public void Register<T>(string eventType)
    {
        _registryByName[eventType] = typeof(T);
        _registryByType[typeof(T)] = eventType;
    }

    public Type GetTypeByName(string eventType)
    {
        return _registryByName[eventType];
    }

    public string GetNameByType(Type eventType)
    {
        return _registryByType[eventType];
    }
}