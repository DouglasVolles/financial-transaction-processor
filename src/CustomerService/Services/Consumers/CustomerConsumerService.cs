using CustomerService.Data;
using CustomerService.Models;
using CustomerService.Services.CustomerCreation;
using CustomerService.Services.Messaging;
using System.Text.Json;

namespace CustomerService.Services.Consumers;

public class CustomerConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<CustomerConsumerService> _logger;

    public CustomerConsumerService(
        IServiceScopeFactory scopeFactory,
        IRabbitMqService rabbitMqService,
        ILogger<CustomerConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EventHandler<string> onMessageReceived = async (_, message) =>
        {
            await ProcessMessageAsync(message, stoppingToken);
        };

        _rabbitMqService.MessageReceived += onMessageReceived;

        _rabbitMqService.StartConsuming();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Customer consumer stopping due to cancellation request.");
        }
        finally
        {
            _rabbitMqService.MessageReceived -= onMessageReceived;
        }
    }

    private async Task ProcessMessageAsync(string message, CancellationToken stoppingToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CustomerRequest>(message);
            if (request is null)
            {
                _logger.LogWarning("Message ignored because payload could not be deserialized. Payload: {Payload}", message);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var creationService = scope.ServiceProvider.GetRequiredService<ICustomerCreationService>();
            var result = await creationService.CreateAsync(request, stoppingToken);

            if (result.Status == CustomerCreationStatus.Created)
            {
                _logger.LogInformation("Customer saved successfully. CustomerId: {CustomerId}", result.CustomerId);
                return;
            }

            _logger.LogWarning(
                "Message processed without persistence due to business rule. Status: {Status}. Reason: {Reason}. CpfCnpj: {CpfCnpj}",
                result.Status,
                result.Message,
                request.CpfCnpj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer message");
        }
    }
}
