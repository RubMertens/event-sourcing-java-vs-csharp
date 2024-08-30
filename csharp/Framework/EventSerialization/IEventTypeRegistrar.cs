namespace Framework;

public interface IEventTypeRegistrar
{
    IEventTypeRegistrar Register<T>(string eventType);
}