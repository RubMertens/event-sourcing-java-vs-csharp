using System.Reflection;

namespace Framework.EventSerialization;

public class EventTypeRegistry : IEventTypeRegistrar, IEventTypeRegistry
{
    private Dictionary<string, Type> _registryByName = new();
    private Dictionary<Type, string> _registryByType = new();

    public IEventTypeRegistrar Register<T>(string eventType)
    {
        Register(typeof(T), eventType);
        return this;
    }

    private void Register(Type type, string eventType)
    {
        _registryByName[eventType] = type;
        _registryByType[type] = eventType;
    }

    public IEventTypeRegistrar RegisterAllInAssemblyOf<TTypeInAssembly>()
    {
        var assembly = typeof(TTypeInAssembly).Assembly;
        var types = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<EventTypeAttribute>() != null)
            ;
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<EventTypeAttribute>();
            Register(type, attribute.Name);
        }

        return this;
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

[AttributeUsage(AttributeTargets.Class)]
public class EventTypeAttribute : Attribute
{
    public string Name { get; }

    public EventTypeAttribute(string name)
    {
        Name = name;
    }
}