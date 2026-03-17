using System.Globalization;
using AccountService.Models;
using AccountService.Services.Transactions.Rules;
using FluentAssertions;
using Reqnroll;

namespace AccountService.UnitTests.Steps;

[Binding]
public sealed class TransactionRuleHandlerSteps
{
    private Account _sourceAccount = null!;
    private Account? _destinationAccount;
    private Transaction? _lastTransaction;
    private TransactionRuleResult _result = null!;

    [Given(@"a source account with available balance (.*), reserved balance (.*) and credit limit (.*)")]
    public void GivenASourceAccountWith(string availableBalance, string reservedBalance, string creditLimit)
    {
        _sourceAccount = new Account
        {
            Identification = "ACC-SRC",
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture),
            ReservedBalance = decimal.Parse(reservedBalance, CultureInfo.InvariantCulture),
            CreditLimit = decimal.Parse(creditLimit, CultureInfo.InvariantCulture)
        };
    }

    [Given(@"a destination account with available balance (.*)")]
    public void GivenADestinationAccountWith(string availableBalance)
    {
        _destinationAccount = new Account
        {
            Identification = "ACC-DST",
            AvailableBalance = decimal.Parse(availableBalance, CultureInfo.InvariantCulture)
        };
    }

    [Given(@"no destination account is specified")]
    public void GivenNoDestinationAccountIsSpecified()
    {
        _destinationAccount = null;
    }

    [Given(@"no previous transaction is defined")]
    public void GivenNoPreviousTransactionIsDefined()
    {
        _lastTransaction = null;
    }

    [Given(@"the last transaction was a ""(.*)"" of (.*)")]
    public void GivenTheLastTransactionWasA(string operation, string amount)
    {
        _lastTransaction = new Transaction
        {
            Operation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true),
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture)
        };
    }

    [When(@"I apply the ""(.*)"" rule handler with operation ""(.*)"" and amount (.*)")]
    public async Task WhenIApplyTheRuleHandlerWithOperationAndAmount(string handlerName, string operation, string amount)
    {
        var parsedOperation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true);
        var parsedAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);

        var context = new TransactionRuleContext
        {
            Request = new TransactionRequest { Operation = parsedOperation, AccountId = _sourceAccount.Identification, Currency = "BRL", ReferenceId = "TX-STEP" },
            SourceAccount = _sourceAccount,
            TransactionEntity = new Transaction { Amount = parsedAmount, Operation = parsedOperation }
        };
        context.DestinationAccount = _destinationAccount;
        context.LastTransaction = _lastTransaction;

        _result = await CreateHandler(handlerName).HandleAsync(context, CancellationToken.None);
    }

    [When(@"I apply the rule engine with operation ""(.*)"" and amount (.*)")]
    public async Task WhenIApplyTheRuleEngineWithOperationAndAmount(string operation, string amount)
    {
        var parsedOperation = Enum.Parse<TransactionOperation>(operation, ignoreCase: true);
        var parsedAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);

        var context = new TransactionRuleContext
        {
            Request = new TransactionRequest { Operation = parsedOperation, AccountId = _sourceAccount.Identification, Currency = "BRL", ReferenceId = "TX-ENGINE-STEP" },
            SourceAccount = _sourceAccount,
            TransactionEntity = new Transaction { Amount = parsedAmount, Operation = parsedOperation }
        };
        context.DestinationAccount = _destinationAccount;
        context.LastTransaction = _lastTransaction;

        var engine = new TransactionRuleEngine(
            new CreditTransactionRuleHandler(),
            new DebitTransactionRuleHandler(),
            new ReserveTransactionRuleHandler(),
            new CaptureTransactionRuleHandler(),
            new ReversalTransactionRuleHandler(),
            new TransferTransactionRuleHandler());

        _result = await engine.ApplyAsync(context, CancellationToken.None);
    }

    [Then(@"the rule result should succeed")]
    public void ThenTheRuleResultShouldSucceed()
    {
        _result.IsSuccess.Should().BeTrue();
    }

    [Then(@"the rule result should fail")]
    public void ThenTheRuleResultShouldFail()
    {
        _result.IsSuccess.Should().BeFalse();
    }

    [Then(@"the rule result should fail with message containing ""(.*)""")]
    public void ThenTheRuleResultShouldFailWithMessageContaining(string expectedMessage)
    {
        _result.IsSuccess.Should().BeFalse();
        _result.ErrorMessage.Should().Contain(expectedMessage);
    }

    [Then(@"the source account available balance should be (.*)")]
    public void ThenTheSourceAccountAvailableBalanceShouldBe(string expectedBalance)
    {
        _sourceAccount.AvailableBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }

    [Then(@"the source account reserved balance should be (.*)")]
    public void ThenTheSourceAccountReservedBalanceShouldBe(string expectedBalance)
    {
        _sourceAccount.ReservedBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }

    [Then(@"the destination account available balance should be (.*)")]
    public void ThenTheDestinationAccountAvailableBalanceShouldBe(string expectedBalance)
    {
        _destinationAccount!.AvailableBalance.Should().Be(decimal.Parse(expectedBalance, CultureInfo.InvariantCulture));
    }

    private static ITransactionRuleHandler CreateHandler(string handlerName) => handlerName.ToLower() switch
    {
        "credit" => new CreditTransactionRuleHandler(),
        "debit" => new DebitTransactionRuleHandler(),
        "reserve" => new ReserveTransactionRuleHandler(),
        "capture" => new CaptureTransactionRuleHandler(),
        "reversal" => new ReversalTransactionRuleHandler(),
        "transfer" => new TransferTransactionRuleHandler(),
        _ => throw new ArgumentException($"Unknown handler: {handlerName}")
    };
}
