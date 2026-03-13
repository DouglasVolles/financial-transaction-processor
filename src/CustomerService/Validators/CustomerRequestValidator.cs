using CustomerService.Models;
using FluentValidation;

namespace CustomerService.Validators;

public class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    private static readonly IReadOnlyDictionary<int, Func<string, bool>> DocumentValidatorsByLength =
        new Dictionary<int, Func<string, bool>>
        {
            [11] = IsValidCpf,
            [14] = IsValidCnpj
        };

    public CustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .Length(1, 255)
            .WithMessage("Name must be between 1 and 255 characters");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty()
            .WithMessage("CpfCnpj is required")
            .Length(1, 20)
            .WithMessage("CpfCnpj must be between 1 and 20 characters")
            .Must(BeValidCpfOrCnpj)
            .WithMessage("CpfCnpj must be a valid CPF or CNPJ");
    }

    private static bool BeValidCpfOrCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = ExtractDigits(value);

        return DocumentValidatorsByLength.TryGetValue(digits.Length, out var validator)
            && validator(digits);
    }

    private static string ExtractDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cpf.Select(c => c - '0').ToArray();

        var firstCheckDigit = CalculateCpfCheckDigit(numbers, 9, 10);
        numbers[9] = firstCheckDigit;

        var secondCheckDigit = CalculateCpfCheckDigit(numbers, 10, 11);
        var calculatedCheckDigits = firstCheckDigit * 10 + secondCheckDigit;
        var informedCheckDigits = int.Parse(cpf[^2..]);

        return calculatedCheckDigits == informedCheckDigits;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cnpj.Select(c => c - '0').ToArray();
        var firstWeights = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var secondWeights = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var firstCheckDigit = CalculateCnpjCheckDigit(numbers, firstWeights);
        if (numbers[12] != firstCheckDigit)
        {
            return false;
        }

        numbers[12] = firstCheckDigit;
        var secondCheckDigit = CalculateCnpjCheckDigit(numbers, secondWeights);
        return numbers[13] == secondCheckDigit;
    }

    private static int CalculateCpfCheckDigit(int[] numbers, int length, int initialWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (initialWeight - i);
        }

        return CalculateMod11FromSum(sum);
    }

    private static int CalculateCnpjCheckDigit(int[] numbers, int[] weights)
    {
        var sum = weights.Select((weight, index) => weight * numbers[index]).Sum();
        return CalculateMod11FromSum(sum);
    }

    private static int CalculateMod11FromSum(int sum)
    {
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
