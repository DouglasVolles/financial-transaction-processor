using AccountService.Models;
using AccountService.Validators;
using FluentAssertions;

namespace AccountService.UnitTests.Validators;

public class TransactionRequestValidatorTransferTests
{
    [Fact]
    public void Validate_ShouldFailTransferWhenDestinationFieldIsMissing()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Transfer,
            AccountId = "ACC-001",
            Amount = 1000,
            Currency = "BRL",
            ReferenceId = "TXN-TRANSFER-MISSING"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.DestinationAccountId));
    }

    [Fact]
    public void Validate_ShouldPassTransferWhenDestinationFieldContainsAccountIdentification()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Transfer,
            AccountId = "ACC-001",
            DestinationAccountId = "ACC-002",
            Amount = 1000,
            Currency = "BRL",
            ReferenceId = "TXN-TRANSFER-OK"
        });

        result.IsValid.Should().BeTrue();
    }
}
