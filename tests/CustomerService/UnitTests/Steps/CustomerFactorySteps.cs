using CustomerService.Models;
using CustomerService.Services.CustomerCreation;
using FluentAssertions;
using Reqnroll;

namespace CustomerService.UnitTests.Steps;

[Binding]
public sealed class CustomerFactorySteps
{
    private readonly ICustomerFactory _factory = new CustomerFactory();
    private Customer _customer = null!;

    [Given(@"a customer factory")]
    public void GivenACustomerFactory()
    {
    }

    [When("I create customer from name \"(.*)\" cpfCnpj \"(.*)\" and timestamp \"(.*)\"")]
    public void WhenICreateCustomerFromNameCpfCnpjAndTimestamp(string name, string cpfCnpj, string timestamp)
    {
        _customer = _factory.Create(new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        }, DateTime.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    [Then("the created customer name should be \"(.*)\"")]
    public void ThenTheCreatedCustomerNameShouldBe(string expectedName)
    {
        _customer.Name.Should().Be(expectedName);
    }

    [Then("the created customer cpfCnpj should be \"(.*)\"")]
    public void ThenTheCreatedCustomerCpfCnpjShouldBe(string expectedCpfCnpj)
    {
        _customer.CpfCnpj.Should().Be(expectedCpfCnpj);
    }

    [Then("the created customer timestamp should be \"(.*)\"")]
    public void ThenTheCreatedCustomerTimestampShouldBe(string expectedTimestamp)
    {
        var expected = DateTime.Parse(expectedTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
        _customer.CreatedAt.Should().Be(expected);
    }
}
