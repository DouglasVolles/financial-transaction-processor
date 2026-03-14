namespace AccountService.Services.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string QueueName { get; set; } = "accounts";
    public string ExchangeName { get; set; } = "accounts_exchange";
    public string RoutingKey { get; set; } = "accounts.create";
}

