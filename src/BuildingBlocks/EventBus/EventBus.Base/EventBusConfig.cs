namespace EventBus.Base;

public class EventBusConfig
{
    public int ConnectionRetryCount { get; } = 5;
    public string DefaultTopicName { get; set; } = "FrowiEventBus";
    public string EventBusConnectionString { get; set; } = string.Empty;
    public string SubscribertClientAppName { get; set; } = string.Empty;
    public string EventNamePrefix { get; set; } = string.Empty;
    public string EventNameSuffix { get; set; } = "IntegrationEvent";
    public EventBusType EventBusType { get; set; } = EventBusType.RabbitMq;
    public object Connection { get; set; }
    public bool IsDeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);
    public bool IsDeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);
}

public enum EventBusType
{
    RabbitMq = 0,
    AzureServiceBus = 1
}