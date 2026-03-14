using AccountService.Models;
using FluentValidation;

namespace AccountService.Validators;

public class AccountRequestValidator : AbstractValidator<AccountRequest>
{
    public AccountRequestValidator()
    {
        RuleFor(x => x.CustomerCpFCnpj)
            .NotEmpty()
            .WithMessage("CustomerCpFCnpj is required")
            .MaximumLength(20)
            .WithMessage("CustomerCpFCnpj must be at most 20 characters");

        RuleFor(x => x.AvailableBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("AvailableBalance must be greater than or equal to 0");

        RuleFor(x => x.ReservedBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ReservedBalance must be greater than or equal to 0");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CreditLimit must be greater than or equal to 0");
    }
}
