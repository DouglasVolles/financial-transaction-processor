namespace CustomerService.Services.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string QueueName { get; set; } = "customer";
    public string ExchangeName { get; set; } = "customer_exchange";
    public string RoutingKey { get; set; } = "customer.create";
}
