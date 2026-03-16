using System.Net.Http.Json;
using System.Text.Json;
using AccountService.Data;
using AccountService.IntegrationTests.Support;
using AccountService.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace AccountService.IntegrationTests.Steps;

[Binding]
public sealed class TransactionsApiIntegrationSteps : IDisposable
{
    private readonly IntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage _response = null!;

    public TransactionsApiIntegrationSteps()
    {
        _factory = new IntegrationWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Given("the transaction database is empty")]
    public async Task GivenTheTransactionDatabaseIsEmpty()
    {
        await ResetDatabaseAsync();
    }

    [Given("the transaction database has transaction with reference id \"(.*)\" and status \"(.*)\"")]
    public async Task GivenTheTransactionDatabaseHasTransactionWithReferenceIdAndStatus(string referenceId, string status)
    {
        await ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        dbContext.Transactions.Add(new Transaction
        {
            Operation = TransactionOperation.Credit,
            AccountIdentification = "ACC-001",
            Amount = 100,
            Currency = "BRL",
            ReferenceId = referenceId,
            Metadata = "{}",
            Status = Enum.Parse<TransactionStatus>(status, ignoreCase: true),
            Timestamp = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    [When("I send POST request to \"(.*)\" with operation \"(.*)\", account id \"(.*)\", amount (.*), currency \"(.*)\", reference id \"(.*)\" and metadata \"(.*)\"")]
    public async Task WhenISendPostRequestForTransaction(string path, string operation, string accountId, int amount, string currency, string referenceId, string metadata)
    {
        var normalizedMetadata = metadata.Replace("\\\"", "\"");
        using var metadataDoc = JsonDocument.Parse(normalizedMetadata);

        var payload = new TransactionRequest
        {
            Operation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true),
            AccountId = accountId,
            Amount = amount,
            Currency = currency,
            ReferenceId = referenceId,
            Metadata = metadataDoc.RootElement.Clone()
        };

        _response = await _client.PostAsJsonAsync(path, payload);
    }

    [When("I send GET request for transactions to \"(.*)\"")]
    public async Task WhenISendGetRequestForTransactionsTo(string path)
    {
        _response = await _client.GetAsync(path);
    }

    [Then("the response should contain (.*) transactions")]
    public async Task ThenTheResponseShouldContainTransactions(int expectedCount)
    {
        var transactions = await ReadTransactionListResponseAsync();

        transactions.Should().NotBeNull();
        transactions?.Should().HaveCount(expectedCount);
    }

    [Then("the response should contain transaction id \"(.*)\"")]
    public async Task ThenTheResponseShouldContainTransactionId(string expectedTransactionId)
    {
        var transactions = await ReadTransactionListResponseAsync();
        transactions.Should().Contain(t => t.TransactionId == expectedTransactionId);
    }

    [Then("the response should contain transaction status \"(.*)\"")]
    public async Task ThenTheResponseShouldContainTransactionStatus(string expectedStatus)
    {
        var expected = Enum.Parse<TransactionStatus>(expectedStatus, ignoreCase: true);
        var transactions = await ReadTransactionListResponseAsync();
        transactions.Should().Contain(t => t.Status == expected);
    }

    [Then("the transaction response status code should be (.*)")]
    public void ThenTheTransactionResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        ((int)_response.StatusCode).Should().Be(expectedStatusCode);
    }

    [Then("the transaction response status should be \"(.*)\"")]
    public async Task ThenTheTransactionResponseStatusShouldBe(string expectedStatus)
    {
        var transaction = await ReadTransactionResponseAsync();
        transaction.Status.Should().Be(Enum.Parse<TransactionStatus>(expectedStatus, ignoreCase: true));
    }

    [Then("the transaction response id should be \"(.*)\"")]
    public async Task ThenTheTransactionResponseIdShouldBe(string expectedId)
    {
        var transaction = await ReadTransactionResponseAsync();
        transaction.TransactionId.Should().Be(expectedId);
    }

    [Then("the transaction queue should contain (.*) published message")]
    public void ThenTheTransactionQueueShouldContainPublishedMessage(int expectedCount)
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.PublishedMessages.Should().HaveCount(expectedCount);
    }

    [Then("the last published transaction queue name should be \"(.*)\"")]
    public void ThenTheLastPublishedTransactionQueueNameShouldBe(string expectedQueue)
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.PublishedMessages.Should().NotBeEmpty();
        queue.PublishedMessages[^1].QueueName.Should().Be(expectedQueue);
    }

    private async Task<TransactionResponse> ReadTransactionResponseAsync()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var transaction = JsonSerializer.Deserialize<TransactionResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        transaction.Should().NotBeNull();
        return transaction!;
    }

    private async Task<List<TransactionResponse>> ReadTransactionListResponseAsync()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var transactions = JsonSerializer.Deserialize<List<TransactionResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        transactions.Should().NotBeNull();
        return transactions!;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
