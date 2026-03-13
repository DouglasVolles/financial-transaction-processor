using CustomerService.Services.Messaging;
using System.Text.Json;

namespace CustomerService.IntegrationTests.Support;

public sealed class TestRabbitMqService : IRabbitMqService
{
    public event EventHandler<string>? MessageReceived;

    public List<(object Message, string QueueName)> PublishedMessages { get; } = new();

    public void PublishMessage(object message, string queueName = "customer")
    {
        PublishedMessages.Add((message, queueName));
        MessageReceived?.Invoke(this, JsonSerializer.Serialize(message));
    }

    public void StartConsuming()
    {
    }

    public void Clear() => PublishedMessages.Clear();
}
