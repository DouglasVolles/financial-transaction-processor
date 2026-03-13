using CustomerService.Data;
using CustomerService.Models;
using CustomerService.Services.CustomerCreation;
using CustomerService.UnitTests.Support;
using FluentAssertions;
using Reqnroll;

namespace CustomerService.UnitTests.Steps;

[Binding]
public sealed class CustomerCreationServiceSteps
{
    private CustomerDbContext _dbContext = null!;
    private ICustomerCreationService _service = null!;
    private CustomerRequest _request = null!;
    private CustomerCreationResult _result = null!;
    private int _initialCount;

    [Given(@"a clean customer database")]
    public void GivenACleanCustomerDatabase()
    {
        _dbContext = TestDbContextFactory.Create();
        _service = new CustomerCreationService(_dbContext, new CustomerFactory());
        _initialCount = _dbContext.Customers.Count();
    }

    [Given("a clean customer database with an existing customer cpfCnpj \"(.*)\"")]
    public async Task GivenACleanCustomerDatabaseWithAnExistingCustomerCpfCnpj(string cpfCnpj)
    {
        GivenACleanCustomerDatabase();
        _dbContext.Customers.Add(new Customer
        {
            Name = "Existing Customer",
            CpfCnpj = cpfCnpj,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        _initialCount = _dbContext.Customers.Count();
    }

    [Given("a valid customer request with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void GivenAValidCustomerRequest(string name, string cpfCnpj)
    {
        _request = new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        };
    }

    [Given("an invalid customer request with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void GivenAnInvalidCustomerRequest(string name, string cpfCnpj)
    {
        GivenAValidCustomerRequest(name, cpfCnpj);
    }

    [When(@"I create the customer")]
    public async Task WhenICreateTheCustomer()
    {
        _result = await _service.CreateAsync(_request, CancellationToken.None);
    }

    [Then("the creation status should be \"(.*)\"")]
    public void ThenTheCreationStatusShouldBe(string expectedStatus)
    {
        Enum.Parse<CustomerCreationStatus>(expectedStatus).Should().Be(_result.Status);
    }

    [Then("one customer should be persisted with normalized cpfCnpj \"(.*)\"")]
    public void ThenOneCustomerShouldBePersistedWithNormalizedCpfCnpj(string normalizedCpfCnpj)
    {
        _dbContext.Customers.Should().HaveCount(1);

        var customer = _dbContext.Customers.Single();
        customer.CpfCnpj.Should().Be(normalizedCpfCnpj);
    }

    [Then(@"no customers should be persisted")]
    public void ThenNoCustomersShouldBePersisted()
    {
        _dbContext.Customers.Count().Should().Be(_initialCount);
    }

    [Then(@"no additional customers should be persisted")]
    public void ThenNoAdditionalCustomersShouldBePersisted()
    {
        _dbContext.Customers.Count().Should().Be(_initialCount);
    }
}
