using System.Net.Http.Json;
using System.Text.Json;
using CustomerService.Data;
using CustomerService.Models;
using CustomerService.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace CustomerService.IntegrationTests.Steps;

[Binding]
public sealed class CustomersApiIntegrationSteps : IDisposable
{
    private readonly IntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage _response = null!;

    public CustomersApiIntegrationSteps()
    {
        _factory = new IntegrationWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Given(@"the customer database is empty")]
    public async Task GivenTheCustomerDatabaseIsEmpty()
    {
        await ResetDatabaseAsync();
    }

    [Given(@"the customer database has (.*) customers")]
    public async Task GivenTheCustomerDatabaseHasCustomers(int count)
    {
        await ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        for (var i = 1; i <= count; i++)
        {
            dbContext.Customers.Add(new Customer
            {
                Name = $"Customer {i}",
                CpfCnpj = $"1111111111{i}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    [Given("the customer database has customer id (.*) name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public async Task GivenTheCustomerDatabaseHasCustomerIdNameAndCpfCnpj(int id, string name, string cpfCnpj)
    {
        await ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        dbContext.Customers.Add(new Customer
        {
            Id = id,
            Name = name,
            CpfCnpj = cpfCnpj,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    [Given(@"the customer queue is empty")]
    public void GivenTheCustomerQueueIsEmpty()
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.Clear();
    }

    [When("I send GET request to \"(.*)\"")]
    public async Task WhenISendGetRequestTo(string path)
    {
        _response = await _client.GetAsync(path);
    }

    [When("I send POST request to \"(.*)\" with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public async Task WhenISendPostRequestToWithNameAndCpfCnpj(string path, string name, string cpfCnpj)
    {
        var payload = new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        };

        _response = await _client.PostAsJsonAsync(path, payload);
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        ((int)_response.StatusCode).Should().Be(expectedStatusCode);
    }

    [Then(@"the response should contain (.*) customers")]
    public async Task ThenTheResponseShouldContainCustomers(int expectedCount)
    {
        var content = await _response.Content.ReadAsStringAsync();
        var customers = JsonSerializer.Deserialize<List<CustomerResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        customers.Should().NotBeNull();
        customers!.Should().HaveCount(expectedCount);
    }

    [Then("the response customer name should be \"(.*)\"")]
    public async Task ThenTheResponseCustomerNameShouldBe(string expectedName)
    {
        var content = await _response.Content.ReadAsStringAsync();
        var customer = JsonSerializer.Deserialize<CustomerResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        customer.Should().NotBeNull();
        customer!.Name.Should().Be(expectedName);
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

    [Then(@"the queue should contain (.*) published customer message")]
    public void ThenTheQueueShouldContainPublishedCustomerMessage(int expectedCount)
    {
        var queue = _factory.Services.GetRequiredService<TestRabbitMqService>();
        queue.PublishedMessages.Should().HaveCount(expectedCount);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
