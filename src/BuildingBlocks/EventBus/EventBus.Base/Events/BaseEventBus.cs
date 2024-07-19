using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider _serviceProvider;
    public readonly IEventBusSubscriptionManager _subsManager;
    public EventBusConfig _eventBusConfig { get; set; }

    public BaseEventBus(EventBusConfig eventBusConfig, IServiceProvider serviceprovider)
    {
        _eventBusConfig = eventBusConfig;
        _serviceProvider = serviceprovider;
        _subsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName); 
    }

    public virtual string ProcessEventName(string eventName)
    {
        if (_eventBusConfig.IsDeleteEventPrefix)
            eventName = eventName.TrimStart(_eventBusConfig.EventNamePrefix.ToArray());
        if (_eventBusConfig.IsDeleteEventSuffix)
            eventName = eventName.TrimEnd(_eventBusConfig.EventNameSuffix.ToArray());

        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{_eventBusConfig.SubscribertClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        _eventBusConfig = null;
        _subsManager.Clear();
    }

    public async Task<bool> ProcessEvent(string eventname, string message)
    {
        eventname = ProcessEventName(eventname);
        var processed = false;
        if (_subsManager.HasSubscriptionsForEvent(eventname))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventname);

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = _serviceProvider.GetService(subscription.HandlerType);
                    if(handler is null) continue;

                    var eventType =
                        _subsManager.GetEventTypeByName(
                            $"{_eventBusConfig.EventNamePrefix}{eventname}{_eventBusConfig.EventNameSuffix}");

                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }

            processed = true;
        }

        return processed;
    }

    public abstract void Publish(IntegrationEvent @event);
    public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}