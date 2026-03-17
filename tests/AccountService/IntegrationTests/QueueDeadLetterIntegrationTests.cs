using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AccountService.Data;
using AccountService.Services.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace AccountService.IntegrationTests;

public class QueueDeadLetterIntegrationTests
{
    [Fact]
    public async Task TransactionMessage_WithRetryHeaderAtMax_ShouldBeMovedToDeadLetterQueue()
    {
        await using var factory = new DeadLetterWebApplicationFactory();
        using var client = factory.CreateClient();

        var settings = factory.Services.GetRequiredService<RabbitMqSettings>();
        using var managementClient = CreateRabbitMqManagementClient(settings);

        await EnsureServiceReadyAsync(client);
        await PurgeQueueAsync(managementClient, "transactions");
        await PurgeQueueAsync(managementClient, "transactions-error");

        // Publish with retry header already at max so consumer dead-letters immediately.
        PublishTransactionMessage(settings, retryCount: 3, referenceId: $"deadletter-{Guid.NewGuid():N}");

        var movedToErrorQueue = await WaitForConditionAsync(async () =>
        {
            var error = await GetQueueSnapshotAsync(managementClient, "transactions-error");
            return error.Messages >= 1;
        }, timeout: TimeSpan.FromSeconds(20), pollInterval: TimeSpan.FromMilliseconds(500));

        Assert.True(movedToErrorQueue);

        var transactions = await GetQueueSnapshotAsync(managementClient, "transactions");
        Assert.Equal(0, transactions.MessagesReady);
        Assert.Equal(0, transactions.MessagesUnacknowledged);
    }

    private static async Task EnsureServiceReadyAsync(HttpClient client)
    {
        var ready = await WaitForConditionAsync(async () =>
        {
            try
            {
                using var response = await client.GetAsync("/api/financialtransaction/transactions");
                return response.StatusCode is HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromSeconds(15), pollInterval: TimeSpan.FromMilliseconds(300));

        Assert.True(ready);
    }

    private static HttpClient CreateRabbitMqManagementClient(RabbitMqSettings settings)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:15672/")
        };

        var credentialBytes = Encoding.ASCII.GetBytes($"{settings.UserName}:{settings.Password}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));
        return client;
    }

    private static async Task PurgeQueueAsync(HttpClient client, string queueName)
    {
        var response = await client.DeleteAsync($"api/queues/%2f/{queueName}/contents");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new Xunit.SkipException("RabbitMQ management API is not available for dead-letter integration test.");
        }

        response.EnsureSuccessStatusCode();
    }

    private static void PublishTransactionMessage(RabbitMqSettings settings, int retryCount, string referenceId)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare("transactions_exchange", ExchangeType.Direct, durable: true);
        channel.QueueDeclare("transactions", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind("transactions", "transactions_exchange", "transactions.create");
        channel.QueueDeclare("transactions-error", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind("transactions-error", "transactions_exchange", "transactions.error");

        var payload = JsonSerializer.Serialize(new
        {
            operation = "Credit",
            account_id = "ACC-404",
            amount = 10,
            currency = "BRL",
            reference_id = referenceId,
            metadata = new { description = "dead-letter test" }
        });

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            ["x-retry-count"] = retryCount
        };

        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish("transactions_exchange", "transactions.create", properties, body);
    }

    private static async Task<QueueSnapshot> GetQueueSnapshotAsync(HttpClient client, string queueName)
    {
        var response = await client.GetAsync($"api/queues/%2f/{queueName}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var snapshot = JsonSerializer.Deserialize<QueueSnapshot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return snapshot ?? new QueueSnapshot();
    }

    private static async Task<bool> WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollInterval)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(pollInterval);
        }

        return false;
    }

    private sealed class QueueSnapshot
    {
        public int Messages { get; set; }
        public int MessagesReady { get; set; }
        public int MessagesUnacknowledged { get; set; }
    }

    private sealed class DeadLetterWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"dead-letter-test-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AccountDbContext>));
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AccountDbContext>));
                services.RemoveAll(typeof(AccountDbContext));

                services.AddDbContext<AccountDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
