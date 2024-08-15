namespace Framework;

public interface IEventTypeRegistrar
{
    void Register<T>(string eventType);
}