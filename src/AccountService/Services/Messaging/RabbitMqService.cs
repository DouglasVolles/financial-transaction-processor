using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace AccountService.Services.Messaging;

public interface IRabbitMqService
{
    void PublishMessage(
        object message,
        string queueName = "accounts",
        string? exchangeName = null,
        string? routingKey = null,
        IDictionary<string, object>? headers = null);
    event EventHandler<string>? MessageReceived;
    void StartConsuming();
}

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;
    private readonly RabbitMqSettings _settings;

    public event EventHandler<string>? MessageReceived;

    public RabbitMqService(RabbitMqSettings settings)
    {
        _settings = settings;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing RabbitMQ: {ex.Message}");
        }
    }

    public void PublishMessage(
        object message,
        string queueName = "accounts",
        string? exchangeName = null,
        string? routingKey = null,
        IDictionary<string, object>? headers = null)
    {
        try
        {
            if (_channel == null) return;

            var effectiveExchange = exchangeName ?? _settings.ExchangeName;
            var effectiveRoutingKey = routingKey ?? _settings.RoutingKey;

            _channel.ExchangeDeclare(effectiveExchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, effectiveExchange, effectiveRoutingKey);

            var json = JsonSerializer.Serialize(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Headers = headers;

            _channel.BasicPublish(effectiveExchange, effectiveRoutingKey, properties, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing message: {ex.Message}");
        }
    }

    public void StartConsuming()
    {
        try
        {
            if (_channel == null) return;

            _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_settings.QueueName, _settings.ExchangeName, _settings.RoutingKey);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageString = System.Text.Encoding.UTF8.GetString(body);
                MessageReceived?.Invoke(this, messageString);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(_settings.QueueName, false, consumer: consumer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting consumer: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

