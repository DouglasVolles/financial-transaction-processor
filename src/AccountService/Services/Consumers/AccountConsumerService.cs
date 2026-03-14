using AccountService.Data;
using AccountService.Models;
using AccountService.Services.AccountCreation;
using AccountService.Services.Messaging;
using System.Text.Json;

namespace AccountService.Services.Consumers;

public class AccountConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<AccountConsumerService> _logger;

    public AccountConsumerService(
        IServiceScopeFactory scopeFactory,
        IRabbitMqService rabbitMqService,
        ILogger<AccountConsumerService> logger)
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
            _logger.LogInformation("Account consumer stopping due to cancellation request.");
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
            var request = JsonSerializer.Deserialize<AccountRequest>(message);
            if (request is null)
            {
                _logger.LogWarning("Message ignored because payload could not be deserialized. Payload: {Payload}", message);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var creationService = scope.ServiceProvider.GetRequiredService<IAccountCreationService>();
            var result = await creationService.CreateAsync(request, stoppingToken);

            if (result.Status == AccountCreationStatus.Created)
            {
                _logger.LogInformation("Account saved successfully. AccountId: {AccountId}", result.AccountId);
                return;
            }

            _logger.LogWarning(
                "Message processed without persistence due to business rule. Status: {Status}. Reason: {Reason}.",
                result.Status,
                result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing account message");
        }
    }
}

