using Common.Domain.Events.Orders;
using Domain.Common.States;
using MassTransit;

namespace Orchestration.Middlewares;

// Unregistered for now.
public class OrderStateObserver : IStateObserver<OrderState>
{
    public async Task StateChanged(BehaviorContext<OrderState> context, State currentState, State previousState)
    {
        if (!IsNotifiableState(currentState.Name))
            return;

        var eventTypeName = $"Core.Domain.Events.Order{currentState.Name}Event";

        var eventType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType(eventTypeName))
            .FirstOrDefault(t => t != null);

        if (eventType is null)
        {
            Console.WriteLine($"[Observer] Event type not found: {eventTypeName}");
            return;
        }

        var evt = (OrderEvent)Activator.CreateInstance(eventType)!;
        evt.OrderId = context.Saga.CorrelationId;

        var consume = context.GetPayload<ConsumeContext>();
        await consume.Publish(evt, eventType);
    }

    private static bool IsNotifiableState(string name)
        => name is "Submitted" or "Confirmed" or "Completed" or "Expired";
}
