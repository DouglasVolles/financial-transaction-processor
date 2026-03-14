using System.Net.Http.Json;
using System.Text.Json;
using AccountService.Data;
using AccountService.IntegrationTests.Support;
using AccountService.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using System.Globalization;

namespace AccountService.IntegrationTests.Steps;

[Binding]
public sealed class AccountsApiIntegrationSteps : IDisposable
{
    private readonly IntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage _response = null!;

    public AccountsApiIntegrationSteps()
    {
        _factory = new IntegrationWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Given("the account database is empty")]
    public async Task GivenTheAccountDatabaseIsEmpty()
    {
        await ResetDatabaseAsync();
    }

    [Given("the account database has account id (.*), customer id (.*), identification \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public async Task GivenTheAccountDatabaseHasAccountIdAndName(int id, int customerId, string identification, string availableBalance, string reservedBalance, string creditLimit, string accountStatus)
    {
        await ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        dbContext.Accounts.Add(new Account
        {
            Id = id,
            CustomerId = customerId,
            Identification = identification,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = Enum.Parse<AccountStatus>(accountStatus)
        });

        await dbContext.SaveChangesAsync();
    }

    [Given("the account queue is empty")]
    public void GivenTheAccountQueueIsEmpty()
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.Clear();
    }

    [When("I send GET request to \"(.*)\"")]
    public async Task WhenISendGetRequestTo(string path)
    {
        _response = await _client.GetAsync(path);
    }

    [When("I send POST request to \"(.*)\" with customer cpfcnpj \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public async Task WhenISendPostRequestToWithName(string path, string customerCpFCnpj, string availableBalance, string reservedBalance, string creditLimit, string accountStatus)
    {
        var payload = new AccountRequest
        {
            CustomerCpFCnpj = customerCpFCnpj,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = Enum.Parse<AccountStatus>(accountStatus)
        };
        _response = await _client.PostAsJsonAsync(path, payload);
    }

    [Then("the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        ((int)_response.StatusCode).Should().Be(expectedStatusCode);
    }

    [Then("the response should contain (.*) accounts")]
    public async Task ThenTheResponseShouldContainAccounts(int expectedCount)
    {
        var content = await _response.Content.ReadAsStringAsync();
        var accounts = JsonSerializer.Deserialize<List<AccountResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        accounts.Should().NotBeNull();
        accounts!.Should().HaveCount(expectedCount);
    }

    [Then("the response account id should be (.*)")]
    public async Task ThenTheResponseAccountIdShouldBe(int expectedId)
    {
        var account = await ReadAccountResponseAsync();
        account.Id.Should().Be(expectedId);
    }

    [Then("the response customer id should be (.*)")]
    public async Task ThenTheResponseCustomerIdShouldBe(int expectedCustomerId)
    {
        var account = await ReadAccountResponseAsync();
        account.CustomerId.Should().Be(expectedCustomerId);
    }

    [Then("the response identification should be \"(.*)\"")]
    public async Task ThenTheResponseIdentificationShouldBe(string expectedIdentification)
    {
        var account = await ReadAccountResponseAsync();
        account.Identification.Should().Be(expectedIdentification);
    }

    [Then("the response available balance should be (.*)")]
    public async Task ThenTheResponseAvailableBalanceShouldBe(string expectedBalance)
    {
        var account = await ReadAccountResponseAsync();
        account.AvailableBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }

    [Then("the response reserved balance should be (.*)")]
    public async Task ThenTheResponseReservedBalanceShouldBe(string expectedBalance)
    {
        var account = await ReadAccountResponseAsync();
        account.ReservedBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }

    [Then("the response credit limit should be (.*)")]
    public async Task ThenTheResponseCreditLimitShouldBe(string expectedCreditLimit)
    {
        var account = await ReadAccountResponseAsync();
        account.CreditLimit.Should().Be(decimal.Parse(expectedCreditLimit, CultureInfo.InvariantCulture));
    }

    [Then("the response account status should be \"(.*)\"")]
    public async Task ThenTheResponseAccountStatusShouldBe(string expectedStatus)
    {
        var account = await ReadAccountResponseAsync();
        account.AccountStatus.Should().Be(Enum.Parse<AccountStatus>(expectedStatus));
    }

    [Then("the response should contain validation error for field \"(.*)\"")]
    public async Task ThenTheResponseShouldContainValidationErrorForField(string field)
    {
        var content = await _response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty(field, out var fieldErrors).Should().BeTrue();
        fieldErrors.ValueKind.Should().Be(JsonValueKind.Array);
        fieldErrors.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Then("the response should contain validation message for field \"(.*)\" with value \"(.*)\"")]
    public async Task ThenTheResponseShouldContainValidationMessageForFieldWithValue(string field, string message)
    {
        var content = await _response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty(field, out var fieldErrors).Should().BeTrue();
        fieldErrors.ValueKind.Should().Be(JsonValueKind.Array);

        var values = fieldErrors.EnumerateArray().Select(x => x.GetString()).ToList();
        values.Should().Contain(message);
    }

    [Then("the queue should contain (.*) published account message")]
    public void ThenTheQueueShouldContainPublishedAccountMessage(int expectedCount)
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.PublishedMessages.Should().HaveCount(expectedCount);
    }

    [Then("the last published account queue name should be \"(.*)\"")]
    public void ThenTheLastPublishedAccountQueueNameShouldBe(string expectedQueue)
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.PublishedMessages.Should().NotBeEmpty();
        queue.PublishedMessages[^1].QueueName.Should().Be(expectedQueue);
    }

    private async Task<AccountResponse> ReadAccountResponseAsync()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var account = JsonSerializer.Deserialize<AccountResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        account.Should().NotBeNull();
        return account!;
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
