using System.Text;
using System.Text.Json;
using AccountService.Models;
using AccountService.Services.Messaging;
using AccountService.Services.Transactions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AccountService.Services.Consumers;

public sealed class TransactionConsumerService : BackgroundService
{
    private const string TransactionsExchange = "transactions_exchange";
    private const string TransactionsQueue = "transactions";
    private const string TransactionsRoutingKey = "transactions.create";
    private const string TransactionsErrorQueue = "transactions-error";
    private const string TransactionsErrorRoutingKey = "transactions.error";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<TransactionConsumerService> _logger;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    private IConnection? _connection;
    private IModel? _channel;

    public TransactionConsumerService(
        IServiceScopeFactory scopeFactory,
        RabbitMqSettings settings,
        ILogger<TransactionConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _logger.LogInformation(
            "Transaction consumer connected to RabbitMQ at {Host}:{Port}.",
            _settings.HostName,
            _settings.Port);

        _channel.ExchangeDeclare(TransactionsExchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(TransactionsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(TransactionsQueue, TransactionsExchange, TransactionsRoutingKey);

        _channel.QueueDeclare(TransactionsErrorQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(TransactionsErrorQueue, TransactionsExchange, TransactionsErrorRoutingKey);

        _channel.BasicQos(0, 1, false);

        _logger.LogInformation(
            "Transaction consumer topology ready. Queue={Queue}, ErrorQueue={ErrorQueue}, Exchange={Exchange}.",
            TransactionsQueue,
            TransactionsErrorQueue,
            TransactionsExchange);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, eventArgs) =>
        {
            await _processingLock.WaitAsync(stoppingToken);
            try
            {
                await HandleMessageAsync(eventArgs, stoppingToken);
            }
            finally
            {
                _processingLock.Release();
            }
        };

        _channel.BasicConsume(TransactionsQueue, false, consumer);

        _logger.LogInformation("Transaction consumer started consuming queue {Queue}.", TransactionsQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transaction consumer stopping due to cancellation request.");
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        _logger.LogInformation(
            "Received transaction message. DeliveryTag={DeliveryTag}, Redelivered={Redelivered}.",
            eventArgs.DeliveryTag,
            eventArgs.Redelivered);

        try
        {
            var request = JsonSerializer.Deserialize<TransactionRequest>(message);
            if (request is null)
            {
                throw new InvalidOperationException("Invalid transaction payload");
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessor>();
            var result = await processor.ProcessAsync(request, cancellationToken);

            if (result.Status == TransactionStatus.Success)
            {
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                _logger.LogInformation(
                    "Transaction processed successfully. DeliveryTag={DeliveryTag}, ReferenceId={ReferenceId}.",
                    eventArgs.DeliveryTag,
                    request.ReferenceId);
                return;
            }

            throw new InvalidOperationException(result.ErrorMessage ?? "Unknown transaction processing error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction message.");
            await RetryOrDeadLetterAsync(eventArgs, cancellationToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var currentRetry = GetRetryCount(eventArgs.BasicProperties?.Headers);

            if (currentRetry < 3)
            {
                _logger.LogWarning(
                    "Transaction processing failed. Scheduling retry {NextRetry}/3 in 10s. DeliveryTag={DeliveryTag}.",
                    currentRetry + 1,
                    eventArgs.DeliveryTag);

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                var headers = new Dictionary<string, object>
                {
                    ["x-retry-count"] = currentRetry + 1
                };
                PublishWithNewChannel(TransactionsRoutingKey, eventArgs.Body, headers);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
                _logger.LogInformation(
                    "Retry message published and original acked. DeliveryTag={DeliveryTag}, Retry={Retry}.",
                    eventArgs.DeliveryTag,
                    currentRetry + 1);
                return;
            }

            _logger.LogError(
                "Transaction processing failed after 3 retries. Sending message to dead-letter queue {ErrorQueue}. DeliveryTag={DeliveryTag}.",
                TransactionsErrorQueue,
                eventArgs.DeliveryTag);

            PublishWithNewChannel(TransactionsErrorRoutingKey, eventArgs.Body, headers: null);

            _channel.BasicAck(eventArgs.DeliveryTag, false);
            _logger.LogInformation(
                "Dead-letter message published and original acked. DeliveryTag={DeliveryTag}.",
                eventArgs.DeliveryTag);
        }
        catch (Exception retryEx)
        {
            _logger.LogError(
                retryEx,
                "Retry/dead-letter handling failed for DeliveryTag={DeliveryTag}. Message will be requeued.",
                eventArgs.DeliveryTag);

            try
            {
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx, "Failed to nack message after retry handling failure. DeliveryTag={DeliveryTag}.", eventArgs.DeliveryTag);
            }
        }
    }

    private void PublishWithNewChannel(string routingKey, ReadOnlyMemory<byte> body, IDictionary<string, object>? headers)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("RabbitMQ connection is not available for publish.");
        }

        using var publishChannel = _connection.CreateModel();
        publishChannel.ExchangeDeclare(TransactionsExchange, ExchangeType.Direct, durable: true);

        var properties = publishChannel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = headers;

        publishChannel.BasicPublish(
            exchange: TransactionsExchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);
    }

    private static int GetRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            ReadOnlyMemory<byte> memory when int.TryParse(Encoding.UTF8.GetString(memory.Span), out var parsed) => parsed,
            string text when int.TryParse(text, out var parsed) => parsed,
            byte parsed => parsed,
            sbyte parsed => parsed,
            short parsed => parsed,
            ushort parsed => parsed,
            int parsed => parsed,
            uint parsed => (int)parsed,
            long parsed => (int)parsed,
            ulong parsed => (int)parsed,
            _ => 0
        };
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
