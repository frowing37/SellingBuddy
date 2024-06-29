using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider _serviceProvider;
    public readonly IEventBusSubscriptionManager _subsManager;
    private EventBusConfig _config;

    public BaseEventBus(EventBusConfig config, IServiceProvider serviceprovider)
    {
        _config = config;
        _serviceProvider = serviceprovider;
        _subsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
    }

    public virtual string ProcessEventName(string eventName)
    {
        if (_config.IsDeleteEventPrefix)
            eventName = eventName.TrimStart(_config.EventNamePrefix.ToArray());
        if (_config.IsDeleteEventSuffix)
            eventName = eventName.TrimEnd(_config.EventNameSuffix.ToArray());

        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{_config.SubscribertClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        _config = null;
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
                            $"{_config.EventNamePrefix}{eventname}{_config.EventNameSuffix}");

                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }

            processed = true;
        }

        return processed;
    }
    
    public void Publish(IntegrationEvent @event)
    {
        throw new NotImplementedException();
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        throw new NotImplementedException();
    }

    public void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        throw new NotImplementedException();
    }
}