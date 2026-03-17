using System.Globalization;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Transactions;
using AccountService.Services.Transactions.Rules;
using AccountService.UnitTests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Reqnroll;

namespace AccountService.UnitTests.Steps;

[Binding]
public sealed class TransactionProcessorSteps
{
    private AccountDbContext _dbContext = null!;
    private ITransactionProcessor _processor = null!;
    private TransactionResponse _result = null!;
    private TransactionResponse _firstResult = null!;
    private TransactionResponse _secondResult = null!;

    [Given("a clean transaction processor context")]
    public void GivenACleanTransactionProcessorContext()
    {
        _dbContext = TestDbContextFactory.Create();

        var ruleEngine = new TransactionRuleEngine(
            new CreditTransactionRuleHandler(),
            new DebitTransactionRuleHandler(),
            new ReserveTransactionRuleHandler(),
            new CaptureTransactionRuleHandler(),
            new ReversalTransactionRuleHandler(),
            new TransferTransactionRuleHandler());

        _processor = new TransactionProcessor(_dbContext, ruleEngine, NullLogger<TransactionProcessor>.Instance);
    }

    [Given(@"an account with identification ""(.*)"", available balance (.*), reserved balance (.*) and credit limit (.*)")]
    public async Task GivenAnAccountWith(string identification, string availableBalance, string reservedBalance, string creditLimit)
    {
        GivenACleanTransactionProcessorContext();
        _dbContext.Accounts.Add(new Account
        {
            CustomerId = 1,
            Identification = identification,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = AccountStatus.Active
        });

        await _dbContext.SaveChangesAsync();
    }
    [Given(@"an additional account for the same customer with identification \""(.*)\"", available balance (.*), reserved balance (.*) and credit limit (.*)")]
    public async Task GivenAnAdditionalAccountForTheSameCustomerWith(string identification, string availableBalance, string reservedBalance, string creditLimit)
    {
        var existingCustomerId = _dbContext.Accounts.First().CustomerId;
        _dbContext.Accounts.Add(new Account
        {
            CustomerId = existingCustomerId,
            Identification = identification,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = AccountStatus.Active
        });

        await _dbContext.SaveChangesAsync();
    }
    [Given(@"an additional account with identification ""(.*)"", available balance (.*), reserved balance (.*) and credit limit (.*)")]
    public async Task GivenAnAdditionalAccountWith(string identification, string availableBalance, string reservedBalance, string creditLimit)
    {
        _dbContext.Accounts.Add(new Account
        {
            CustomerId = 2,
            Identification = identification,
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture),
            AccountStatus = AccountStatus.Active
        });

        await _dbContext.SaveChangesAsync();
    }

    [Given(@"a previous successful transaction with operation ""(.*)"", amount (.*), currency ""(.*)"" and reference id ""(.*)"" for account ""(.*)""")]
    public async Task GivenAPreviousSuccessfulTransactionForAccount(string operation, string amount, string currency, string referenceId, string accountIdentification)
    {
        var account = _dbContext.Accounts.Single(a => a.Identification == accountIdentification);

        _dbContext.Transactions.Add(new Transaction
        {
            Operation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true),
            AccountId = account.Id,
            AccountIdentification = account.Identification,
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            AvailableBalance = account.AvailableBalance,
            ReservedBalance = account.ReservedBalance,
            Currency = currency,
            ReferenceId = referenceId,
            Metadata = string.Empty,
            Status = TransactionStatus.Success,
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        });

        await _dbContext.SaveChangesAsync();
    }

    [When(@"I process a transaction with operation \""(.*)\"", account id \""(.*)\"", amount (.*), currency \""(.*)\"" and reference id \""(.*)\""")]
    public async Task WhenIProcessATransaction(string operation, string accountId, int amount, string currency, string referenceId)
    {
        _result = await _processor.ProcessAsync(new TransactionRequest
        {
            Operation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true),
            AccountId = accountId,
            Amount = amount,
            Currency = currency,
            ReferenceId = referenceId
        }, CancellationToken.None);
    }

    [When(@"I process the same transaction twice with operation \""(.*)\"", account id \""(.*)\"", amount (.*), currency \""(.*)\"" and reference id \""(.*)\""")]
    public async Task WhenIProcessTheSameTransactionTwice(string operation, string accountId, int amount, string currency, string referenceId)
    {
        var request = new TransactionRequest
        {
            Operation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true),
            AccountId = accountId,
            Amount = amount,
            Currency = currency,
            ReferenceId = referenceId
        };

        _firstResult = await _processor.ProcessAsync(request, CancellationToken.None);
        _secondResult = await _processor.ProcessAsync(request, CancellationToken.None);
        _result = _secondResult;
    }

    [When(@"I process a transfer transaction with account id \""(.*)\"", destination account id \""(.*)\"", amount (.*), currency \""(.*)\"" and reference id \""(.*)\""")]
    public async Task WhenIProcessATransferTransaction(string accountId, string destinationAccountId, int amount, string currency, string referenceId)
    {
        _result = await _processor.ProcessAsync(new TransactionRequest
        {
            Operation = TransactionOperation.Transfer,
            AccountId = accountId,
            DestinationAccountId = destinationAccountId.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? null
                : destinationAccountId,
            Amount = amount,
            Currency = currency,
            ReferenceId = referenceId
        }, CancellationToken.None);
    }

    [Then(@"the transaction status should be ""(.*)""")]
    public void ThenTheTransactionStatusShouldBe(string expectedStatus)
    {
        Enum.Parse<TransactionStatus>(expectedStatus, ignoreCase: true).Should().Be(_result.Status);
    }

    [Then(@"the first transaction status should be \""(.*)\""")]
    public void ThenTheFirstTransactionStatusShouldBe(string expectedStatus)
    {
        Enum.Parse<TransactionStatus>(expectedStatus, ignoreCase: true).Should().Be(_firstResult.Status);
    }

    [Then(@"the second transaction status should be \""(.*)\""")]
    public void ThenTheSecondTransactionStatusShouldBe(string expectedStatus)
    {
        Enum.Parse<TransactionStatus>(expectedStatus, ignoreCase: true).Should().Be(_secondResult.Status);
    }

    [Then(@"the transaction available balance should be (.*)")]
    public void ThenTheTransactionAvailableBalanceShouldBe(long expectedAvailableBalance)
    {
        _result.AvailableBalance.Should().Be(expectedAvailableBalance);
    }

    [Then(@"the transaction reserved balance should be (.*)")]
    public void ThenTheTransactionReservedBalanceShouldBe(long expectedReservedBalance)
    {
        _result.ReservedBalance.Should().Be(expectedReservedBalance);
    }

    [Then("both transaction ids should be equal")]
    public void ThenBothTransactionIdsShouldBeEqual()
    {
        _secondResult.TransactionId.Should().Be(_firstResult.TransactionId);
    }

    [Then(@"the transaction error message should be \""(.*)\""")]
    public void ThenTheTransactionErrorMessageShouldBe(string expectedError)
    {
        _result.ErrorMessage.Should().Be(expectedError);
    }

    [Then(@"the transaction error message should contain \""(.*)\""")]
    public void ThenTheTransactionErrorMessageShouldContain(string expectedError)
    {
        _result.ErrorMessage.Should().Contain(expectedError);
    }

    [Then(@"the persisted transaction count should be (.*)")]
    public void ThenThePersistedTransactionCountShouldBe(int expectedCount)
    {
        _dbContext.Transactions.Count().Should().Be(expectedCount);
    }

    [Then(@"the persisted account with identification \""(.*)\"" available balance should be (.*)")]
    public void ThenThePersistedAccountWithIdentificationAvailableBalanceShouldBe(string identification, string expectedBalance)
    {
        var account = _dbContext.Accounts.Single(a => a.Identification == identification);
        account.AvailableBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }
}
