using AccountService.Models;
using AccountService.Services.AccountCreation;
using AccountService.UnitTests.Support;
using FluentAssertions;
using Reqnroll;
using System.Globalization;

namespace AccountService.UnitTests.Steps;

[Binding]
public sealed class AccountCreationServiceSteps
{
    private AccountService.Data.AccountDbContext _dbContext = null!;
    private IAccountCreationService _service = null!;
    private AccountRequest _request = null!;
    private AccountCreationResult _result = null!;
    private int _initialCount;

    [Given("a clean account database")]
    public void GivenACleanAccountDatabase()
    {
        _dbContext = TestDbContextFactory.Create();
        _service = new AccountCreationService(_dbContext, new AccountFactory(), new TestCustomerLookupService());
        _initialCount = _dbContext.Accounts.Count();
    }

    [Given("a clean account database with existing account id (.*), customer id (.*), identification \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public async Task GivenACleanAccountDatabaseWithExistingAccount(int id, int customerId, string identification, string availableBalance, string reservedBalance, string creditLimit, string accountStatus)
    {
        GivenACleanAccountDatabase();

        _dbContext.Accounts.Add(new Account
        {
            Id = id,
            CustomerId = customerId,
            Identification = identification,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = Enum.Parse<AccountStatus>(accountStatus)
        });

        await _dbContext.SaveChangesAsync();
        _initialCount = _dbContext.Accounts.Count();
    }

    [Given("a valid account request with customer cpfcnpj \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public void GivenAValidAccountRequest(string customerCpFCnpj, string availableBalance, string reservedBalance, string creditLimit, string accountStatus)
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

    [When("I create the account")]
    public async Task WhenICreateTheAccount()
    {
        _result = await _service.CreateAsync(_request, CancellationToken.None);
    }

    [Then("the account creation status should be \"(.*)\"")]
    public void ThenTheAccountCreationStatusShouldBe(string expectedStatus)
    {
        Enum.Parse<AccountCreationStatus>(expectedStatus).Should().Be(_result.Status);
    }

    [Then("the created account id should be (.*)")]
    public void ThenTheCreatedAccountIdShouldBe(int expectedId)
    {
        _result.AccountId.Should().Be(expectedId);
    }

    [Then("one account should be persisted with id (.*), customer id (.*), identification \"(.*)\", available balance (.*), reserved balance (.*), credit limit (.*) and status \"(.*)\"")]
    public void ThenOneAccountShouldBePersistedWithIdAndName(int expectedId, int expectedCustomerId, string expectedIdentification, string expectedAvailableBalance, string expectedReservedBalance, string expectedCreditLimit, string expectedStatus)
    {
        _dbContext.Accounts.Should().ContainSingle();
        var account = _dbContext.Accounts.Single();
        account.Id.Should().Be(expectedId);
        account.CustomerId.Should().Be(expectedCustomerId);
        account.Identification.Should().Be(expectedIdentification);
        account.AvailableBalance.Should().Be(decimal.Parse(expectedAvailableBalance, CultureInfo.InvariantCulture));
        account.ReservedBalance.Should().Be(decimal.Parse(expectedReservedBalance, CultureInfo.InvariantCulture));
        account.CreditLimit.Should().Be(decimal.Parse(expectedCreditLimit, CultureInfo.InvariantCulture));
        account.AccountStatus.Should().Be(Enum.Parse<AccountStatus>(expectedStatus));
    }

    [Then("the account count should be (.*)")]
    public void ThenTheAccountCountShouldBe(int expectedCount)
    {
        _dbContext.Accounts.Count().Should().Be(expectedCount);
    }
}
