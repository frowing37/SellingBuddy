using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using EventBus.UnitTest.Events.Events;
using EventBus.UnitTest.Events.EventsHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace EventBus.UnitTest;

[TestClass]
public class EventBusTests
{
    private IServiceCollection services;

    public EventBusTests()
    {
        services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
    }

    [TestMethod]
    public void subscribe_event_on_rabbitmq_test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });
        
        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();
        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(); 
        //eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }

    [TestMethod]
    public void send_message_to_rabbitmq_test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });
        
        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();
        eventBus.Publish(new OrderCreatedIntegrationEvent(1));
    }
    
    [TestMethod]
    public void subscribe_event_on_azureservicebus_test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetAzureConfig(), sp);
        });
        
        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();
        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(); 
        //eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }

    [TestMethod]
    public void send_message_to_azureservicebus_test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetAzureConfig(), sp);
        });
        
        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();
        eventBus.Publish(new OrderCreatedIntegrationEvent(1));
    }

    private EventBusConfig GetAzureConfig()
    {
        return new()
        {
            DefaultTopicName = "FrowiEventBus",
            SubscribertClientAppName = "EventBusUnitTest",
            EventBusType = EventBusType.AzureServiceBus,
            EventNameSuffix = "IntegrationEvent",
            EventBusConnectionString = "" // have not
        };
    }
    
    private EventBusConfig GetRabbitMQConfig()
    {
        return new()
        {
            DefaultTopicName = "FrowiEventBus",
            SubscribertClientAppName = "EventBusUnitTest",
            EventBusType = EventBusType.RabbitMq,
            EventNameSuffix = "IntegrationEvent",
            // Connection = new ConnectionFactory()
            // {
            //     HostName = "localhost",
            //     Port = 5672,
            //     UserName = "guest",
            //     Password = "guest"
            // }
        };
    }
}