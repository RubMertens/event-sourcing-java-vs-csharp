using System.Reflection;

namespace Framework;

internal static class ReflectionHelper
{
    private static Dictionary<Type, Dictionary<Type, MethodInfo>>
        MethodsByEventByAggregate =
            new();

    private static MethodInfo FindApplyMethod(Type aggregateType,
        Type eventType)
    {
        var applyMethod = aggregateType.GetMethod("Apply",
            new[] { eventType });


        return applyMethod;
    }

    public static void InvokeApplyMethod(this object aggregate, object @event)
    {
        if (MethodsByEventByAggregate.TryGetValue(aggregate.GetType(),
                out var methodsByEvent))
        {
            if (methodsByEvent.TryGetValue(@event.GetType(),
                    out var applyMethod))
            {
                applyMethod.Invoke(aggregate, [@event]);
                return;
            }
        }

        var method = FindApplyMethod(aggregate.GetType(), @event.GetType());
        if (method == null)
        {
            throw new Exception(
                $"Aggregate {aggregate.GetType().Name} does not have an Apply method for event {@event.GetType().Name}");
        }

        MethodsByEventByAggregate[aggregate.GetType()] =
            MethodsByEventByAggregate.GetValueOrDefault(aggregate.GetType(),
                new Dictionary<Type, MethodInfo>());

        method.Invoke(aggregate, [@event]);
    }
}