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

    [Fact]
    public void Validate_ShouldFailWhenAccountIdIsEmpty()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "",
            Amount = 1000,
            Currency = "BRL",
            ReferenceId = "TXN-001"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.AccountId));
    }

    [Fact]
    public void Validate_ShouldFailWhenAmountIsZero()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 0,
            Currency = "BRL",
            ReferenceId = "TXN-001"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.Amount));
    }

    [Fact]
    public void Validate_ShouldFailWhenAmountIsNegative()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = -100,
            Currency = "BRL",
            ReferenceId = "TXN-001"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.Amount));
    }

    [Fact]
    public void Validate_ShouldFailWhenCurrencyIsEmpty()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 1000,
            Currency = "",
            ReferenceId = "TXN-001"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.Currency));
    }

    [Fact]
    public void Validate_ShouldFailWhenCurrencyHasInvalidLength()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 1000,
            Currency = "US",
            ReferenceId = "TXN-001"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.Currency));
    }

    [Fact]
    public void Validate_ShouldFailWhenReferenceIdIsEmpty()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 1000,
            Currency = "BRL",
            ReferenceId = ""
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TransactionRequest.ReferenceId));
    }

    [Fact]
    public void Validate_ShouldPassWhenAllRequiredFieldsAreValid()
    {
        var validator = new TransactionRequestValidator();

        var result = validator.Validate(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 1000,
            Currency = "BRL",
            ReferenceId = "TXN-VALID"
        });

        result.IsValid.Should().BeTrue();
    }
}
