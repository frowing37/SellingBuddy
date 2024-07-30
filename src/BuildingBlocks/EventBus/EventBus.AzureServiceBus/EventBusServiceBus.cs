using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    private ITopicClient _topicClient;
    private ManagementClient _managementClient;
    private ILogger _logger;

    public EventBusServiceBus(EventBusConfig eventBusConfig, IServiceProvider serviceprovider) : base(eventBusConfig,
        serviceprovider)
    {
        _managementClient = new ManagementClient(eventBusConfig.EventBusConnectionString);
        _topicClient = CreateTopicClient();
    }

    private ITopicClient CreateTopicClient()
    {
        if (_topicClient == null || _topicClient.IsClosedOrClosing)
        {
            _topicClient = new TopicClient(_eventBusConfig.EventBusConnectionString, _eventBusConfig.DefaultTopicName,
                RetryPolicy.Default);
        }

        if (_managementClient.TopicExistsAsync(_eventBusConfig.DefaultTopicName).GetAwaiter().GetResult())// dönen ifade asenkron ancak method asenkron olmadığı için getawaiter ve getresult kullandık
        {
            _managementClient.CreateTopicAsync(_eventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
        }

        return _topicClient;
    }

    public override void Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        var eventStr = JsonConvert.SerializeObject(@event);
        var bodyArr = Encoding.UTF8.GetBytes(eventStr);
        
        var message = new Message()
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = null,
            Label = eventName
        };
        
        _topicClient.SendAsync(message).GetAwaiter().GetResult();
    }

    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptionClient = CreateSubscriptionClientIfNotExists(eventName);
            
            RegisterSubscriptionClientssageHandler(subscriptionClient);
        }
        
        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        
        _subsManager.AddSubscription<T, TH>();
    }

    public override void UnSubscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        try
        {
            var subscriptionClient = CreateSubscriptionClient(eventName);
            
            subscriptionClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            _logger.LogWarning("The messaging entity {eventName} Could not be found",eventName);
        }
        _logger.LogInformation("Unsubscribing from event {eventName}",eventName);
        _subsManager.RemovedSubscription<T,TH>();
    }

    private void RegisterSubscriptionClientssageHandler(ISubscriptionClient subscriptionClient)
    {
        subscriptionClient.RegisterMessageHandler(
            async (message, token) =>
            {
                var eventName = $"{message.Label}";
                var messageData = Encoding.UTF8.GetString(message.Body);

                if (await ProcessEvent(ProcessEventName(eventName), messageData))
                {
                    await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            },
        new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });
    }

    private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        var ex = exceptionReceivedEventArgs.Exception;
        var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
        
        _logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

        return Task.CompletedTask;
    }

    private ISubscriptionClient CreateSubscriptionClientIfNotExists(string eventName)
    {
        var subClient = CreateSubscriptionClient(eventName);

        var exists = _managementClient.SubscriptionExistsAsync(_eventBusConfig.DefaultTopicName, GetSubName(eventName))
            .GetAwaiter().GetResult();
        
        if (!exists)
        {
            _managementClient.CreateSubscriptionAsync(_eventBusConfig.DefaultTopicName, GetSubName(eventName))
                .GetAwaiter().GetResult();
            RemoveDefaultRule(subClient);
        }
        
        CreateRuleIfNotExists(ProcessEventName(eventName), subClient);

        return subClient;
    }

    private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
    {
        bool ruleExists;

        try
        {
            var rule = _managementClient.GetRuleAsync(_eventBusConfig.DefaultTopicName, GetSubName(eventName), eventName)
                .GetAwaiter().GetResult();
            ruleExists = rule != null;
        }
        catch (MessagingEntityNotFoundException)
        {
            // Azure Management Client'ı RuleExists methoduna sahip değilse
            ruleExists = false;
        }

        if (!ruleExists)
        {
            subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter { Label = eventName },
                Name = eventName,
            }).GetAwaiter().GetResult();
        }
    }

    private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
    {
        try
        {
            subscriptionClient.RemoveRuleAsync(RuleDescription.DefaultRuleName).GetAwaiter().GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            _logger.LogWarning("the messaging entity {DefaultRuleName} Could not be found", RuleDescription.DefaultRuleName);
        }
    }

    private SubscriptionClient CreateSubscriptionClient(string eventName)
    {
        return new SubscriptionClient(_eventBusConfig.EventBusConnectionString, _eventBusConfig.DefaultTopicName,
            GetSubName(eventName));
    }

    public override void Dispose()
    {
        base.Dispose();
        _topicClient.CloseAsync().GetAwaiter().GetResult();
        _managementClient.CloseAsync().GetAwaiter().GetResult();
        _topicClient = null;
        _managementClient = null;
    }
}