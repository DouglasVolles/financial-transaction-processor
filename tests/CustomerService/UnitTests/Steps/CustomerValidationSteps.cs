using CustomerService.Filters;
using CustomerService.Models;
using CustomerService.Validators;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace CustomerService.UnitTests.Steps;

[Binding]
public sealed class CustomerValidationSteps
{
    private CustomerRequestValidator _validator = null!;
    private CustomerRequest _request = null!;
    private FluentValidation.Results.ValidationResult _validationResult = null!;

    private ValidationFilter _filter = null!;
    private ActionExecutingContext _actionContext = null!;
    private bool _nextCalled;

    [Given(@"a customer request validator")]
    public void GivenACustomerRequestValidator()
    {
        _validator = new CustomerRequestValidator();
    }

    [When("I validate request with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void WhenIValidateRequest(string name, string cpfCnpj)
    {
        _request = new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        };

        _validationResult = _validator.Validate(_request);
    }

    [Then(@"validation should fail")]
    public void ThenValidationShouldFail()
    {
        _validationResult.IsValid.Should().BeFalse();
    }

    [Then("validation should contain error for field \"(.*)\"")]
    public void ThenValidationShouldContainErrorForField(string field)
    {
        _validationResult.Errors.Should().Contain(e => e.PropertyName == field);
    }

    [Then(@"validation should pass")]
    public void ThenValidationShouldPass()
    {
        _validationResult.IsValid.Should().BeTrue();
    }

    [Given(@"a validation filter configured with customer validator")]
    public void GivenAValidationFilterConfiguredWithCustomerValidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CustomerRequest>, CustomerRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        _filter = new ValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController"
            });

        _actionContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    [Given("an invalid action argument with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void GivenAnInvalidActionArgument(string name, string cpfCnpj)
    {
        _actionContext.ActionArguments["request"] = new CustomerRequest
        {
            Name = name,
            CpfCnpj = cpfCnpj
        };
    }

    [Given("a valid action argument with name \"(.*)\" and cpfCnpj \"(.*)\"")]
    public void GivenAValidActionArgument(string name, string cpfCnpj)
    {
        GivenAnInvalidActionArgument(name, cpfCnpj);
    }

    [When(@"the validation filter executes")]
    public async Task WhenTheValidationFilterExecutes()
    {
        ActionExecutionDelegate next = () =>
        {
            _nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                _actionContext,
                new List<IFilterMetadata>(),
                new object()));
        };

        await _filter.OnActionExecutionAsync(_actionContext, next);
    }

    [Then(@"the filter response status code should be (.*)")]
    public void ThenTheFilterResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        var result = _actionContext.Result as ObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(expectedStatusCode);
    }

    [Then(@"the action delegate should not be executed")]
    public void ThenTheActionDelegateShouldNotBeExecuted()
    {
        _nextCalled.Should().BeFalse();
    }

    [Then(@"the filter should continue to action delegate")]
    public void ThenTheFilterShouldContinueToActionDelegate()
    {
        _nextCalled.Should().BeTrue();
        _actionContext.Result.Should().BeNull();
    }
}
