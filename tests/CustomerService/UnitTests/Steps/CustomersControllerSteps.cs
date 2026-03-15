using CustomerService.Controllers;
using CustomerService.Data;
using CustomerService.Models;
using CustomerService.Services.Messaging;
using CustomerService.UnitTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace CustomerService.UnitTests.Steps;

[Binding]
public sealed class CustomersControllerSteps
{
    private CustomerDbContext _dbContext = null!;
    private CustomersController _controller = null!;
    private readonly Mock<IRabbitMqService> _rabbitMqService = new();
    private readonly Mock<ILogger<CustomersController>> _logger = new();

    private IActionResult _addResponse = null!;
    private ActionResult<CustomerResponse> _getByIdResponse = null!;
    private ActionResult<CustomerResponse> _getByCpfCnpjResponse = null!;
    private ActionResult<List<CustomerResponse>> _getAllResponse = null!;

    [Given(@"a customers controller with a healthy queue service")]
    public void GivenACustomersControllerWithAHealthyQueueService()
    {
        _dbContext = TestDbContextFactory.Create();
        _rabbitMqService.Reset();
        _rabbitMqService.Setup(x => x.PublishMessage(It.IsAny<object>(), It.IsAny<string>()));
        _controller = CreateController();
    }

    [Given(@"a customers controller with a failing queue service")]
    public void GivenACustomersControllerWithAFailingQueueService()
    {
        _dbContext = TestDbContextFactory.Create();
        _rabbitMqService.Reset();
        _rabbitMqService
            .Setup(x => x.PublishMessage(It.IsAny<object>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Queue unavailable"));
        _controller = CreateController();
    }

    [Given("a customers controller with seeded customer id (.*) name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public async Task GivenACustomersControllerWithSeededCustomer(int id, string name, string cpfCnpj)
    {
        _dbContext = TestDbContextFactory.Create();
        _dbContext.Customers.Add(new Customer
        {
            Id = id,
            Name = name,
            CpfCnpj = cpfCnpj,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        _controller = CreateController();
    }

    [Given(@"a customers controller with no customers")]
    public void GivenACustomersControllerWithNoCustomers()
    {
        _dbContext = TestDbContextFactory.Create();
        _controller = CreateController();
    }

    [Given(@"a customers controller with (.*) seeded customers")]
    public async Task GivenACustomersControllerWithSeededCustomers(int count)
    {
        _dbContext = TestDbContextFactory.Create();
        for (var i = 1; i <= count; i++)
        {
            _dbContext.Customers.Add(new Customer
            {
                Id = i,
                Name = $"Customer {i}",
                CpfCnpj = $"0000000000{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await _dbContext.SaveChangesAsync();
        _controller = CreateController();
    }

    [When("I post customer request with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void WhenIPostCustomerRequest(string name, string cpfCnpj)
    {
        _addResponse = _controller.AddCustomer(new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        });
    }

    [When(@"I get customer by id (.*)")]
    public async Task WhenIGetCustomerById(int id)
    {
        _getByIdResponse = await _controller.GetCustomer(id);
    }

    [When(@"I get customer by cpfCnpj ""(.*)""")]
    public async Task WhenIGetCustomerByCpfCnpj(string cpfCnpj)
    {
        _getByCpfCnpjResponse = await _controller.GetCustomerByCpfCnpj(cpfCnpj);
    }

    [When(@"I get all customers")]
    public async Task WhenIGetAllCustomers()
    {
        _getAllResponse = await _controller.GetAllCustomers();
    }

    [Then(@"the add customer response status code should be (.*)")]
    public void ThenTheAddCustomerResponseStatusCodeShouldBe(int statusCode)
    {
        GetStatusCode(_addResponse).Should().Be(statusCode);
    }

    [Then(@"the queue should receive one publish request")]
    public void ThenTheQueueShouldReceiveOnePublishRequest()
    {
        _rabbitMqService.Verify(x => x.PublishMessage(It.IsAny<object>(), "customer"), Times.Once);
    }

    [Then(@"the get customer response status code should be (.*)")]
    public void ThenTheGetCustomerResponseStatusCodeShouldBe(int statusCode)
    {
        var result = _getByIdResponse.Result;
        result.Should().NotBeNull();
        GetStatusCode(result!).Should().Be(statusCode);
    }

    [Then("the returned customer name should be \"(.*)\"")]
    public void ThenTheReturnedCustomerNameShouldBe(string expectedName)
    {
        var result = _getByIdResponse.Result as OkObjectResult;
        result.Should().NotBeNull();

        var payload = result!.Value as CustomerResponse;
        payload.Should().NotBeNull();
        payload!.Name.Should().Be(expectedName);
    }

    [Then(@"the get customer by cpfCnpj response status code should be (.*)")]
    public void ThenTheGetCustomerByCpfCnpjResponseStatusCodeShouldBe(int statusCode)
    {
        var result = _getByCpfCnpjResponse.Result;
        result.Should().NotBeNull();
        GetStatusCode(result!).Should().Be(statusCode);
    }

    [Then("the returned customer by cpfCnpj name should be \"(.*)\"")]
    public void ThenTheReturnedCustomerByCpfCnpjNameShouldBe(string expectedName)
    {
        var result = _getByCpfCnpjResponse.Result as OkObjectResult;
        result.Should().NotBeNull();

        var payload = result!.Value as CustomerResponse;
        payload.Should().NotBeNull();
        payload!.Name.Should().Be(expectedName);
    }

    [Then(@"the get all response status code should be (.*)")]
    public void ThenTheGetAllResponseStatusCodeShouldBe(int statusCode)
    {
        var result = _getAllResponse.Result;
        result.Should().NotBeNull();
        GetStatusCode(result!).Should().Be(statusCode);
    }

    [Then(@"the returned customer count should be (.*)")]
    public void ThenTheReturnedCustomerCountShouldBe(int expectedCount)
    {
        var result = _getAllResponse.Result as OkObjectResult;
        result.Should().NotBeNull();

        var payload = result!.Value as List<CustomerResponse>;
        payload.Should().NotBeNull();
        payload!.Should().HaveCount(expectedCount);
    }

    private CustomersController CreateController() =>
        new(_dbContext, _rabbitMqService.Object, _logger.Object);

    private static int GetStatusCode(IActionResult result) => result switch
    {
        OkObjectResult ok => ok.StatusCode ?? 200,
        ObjectResult objectResult => objectResult.StatusCode ?? 500,
        StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
        _ => throw new InvalidOperationException($"Unsupported action result type: {result.GetType().Name}")
    };
}
