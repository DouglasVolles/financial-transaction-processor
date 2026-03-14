using AccountService.Models;
using AccountService.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Reqnroll;
using System.Globalization;

namespace AccountService.UnitTests.Steps;

[Binding]
public sealed class AccountValidationSteps
{
    private AccountRequest _request = null!;
    private ValidationResult _result = null!;

    [Given("an account request with customer cpfcnpj \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public void GivenAnAccountRequest(string customerCpFCnpj, string availableBalance, string reservedBalance, string creditLimit, string accountStatus)
    {
        _request = new AccountRequest
        {
            CustomerCpFCnpj = customerCpFCnpj,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = Enum.Parse<AccountStatus>(accountStatus)
        };
    }

    [When("I validate the account request")]
    public void WhenIValidateTheAccountRequest()
    {
        var validator = new AccountRequestValidator();
        _result = validator.Validate(_request);
    }

    [Then("the account request should be invalid")]
    public void ThenTheAccountRequestShouldBeInvalid()
    {
        _result.IsValid.Should().BeFalse();
    }

    [Then("the account request should be valid")]
    public void ThenTheAccountRequestShouldBeValid()
    {
        _result.IsValid.Should().BeTrue();
    }

    [Then("the validation should contain error for field \"(.*)\"")]
    public void ThenTheValidationShouldContainErrorForField(string field)
    {
        _result.Errors.Should().Contain(x => x.PropertyName == field);
    }
}
