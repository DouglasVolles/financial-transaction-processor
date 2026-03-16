using AccountService.Models;
using AccountService.Validators;
using FluentAssertions;

namespace AccountService.UnitTests.Validators;

public class TransactionRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldFailWhenOperationIsNull()
    {
        var validator = new TransactionRequestValidator();
        var request = new TransactionRequest
        {
            Operation = null,
            AccountId = "ACC-001",
            Amount = 10000,
            Currency = "BRL",
            ReferenceId = "TXN-NULL-OP"
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(TransactionRequest.Operation)
            && e.ErrorMessage == "Operation is required");
    }
}
