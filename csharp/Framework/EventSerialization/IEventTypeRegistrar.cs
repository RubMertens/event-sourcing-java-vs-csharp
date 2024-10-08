namespace Framework.EventSerialization;

public interface IEventTypeRegistrar
{
    IEventTypeRegistrar Register<T>(string eventType);
    IEventTypeRegistrar RegisterAllInAssemblyOf<T>();
}