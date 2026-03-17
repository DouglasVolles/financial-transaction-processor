using AccountService.Models;
using FluentValidation;

namespace AccountService.Validators;

public class TransactionRequestValidator : AbstractValidator<TransactionRequest>
{
    public TransactionRequestValidator()
    {
        RuleFor(x => x.Operation)
            .NotNull()
            .WithMessage("Operation is required")
            .Must(op => op is not null && Enum.IsDefined(op.Value))
            .WithMessage("Operation must be one of: credit, debit, reserve, capture, reversal, transfer");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId is required")
            .MaximumLength(100)
            .WithMessage("AccountId must be at most 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must have 3 characters");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("ReferenceId is required")
            .MaximumLength(250)
            .WithMessage("ReferenceId must be at most 250 characters");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty()
            .WithMessage("DestinationAccountId is required for transfer operations")
            .MaximumLength(100)
            .WithMessage("DestinationAccountId must be at most 100 characters")
            .When(x => x.Operation == TransactionOperation.Transfer);
    }
}
