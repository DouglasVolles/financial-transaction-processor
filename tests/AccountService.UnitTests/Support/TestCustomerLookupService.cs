using AccountService.Services.CustomerLookup;

namespace AccountService.UnitTests.Support;

internal sealed class TestCustomerLookupService : ICustomerLookupService
{
    private static readonly IReadOnlyDictionary<string, int> CustomerIdsByCpFCnpj = new Dictionary<string, int>
    {
        ["12345678901"] = 101,
        ["98765432100"] = 201,
        ["12345678000195"] = 301
    };

    public Task<int?> FindCustomerIdByCpFCnpjAsync(string customerCpFCnpj, CancellationToken cancellationToken)
    {
        var normalized = new string((customerCpFCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
        if (CustomerIdsByCpFCnpj.TryGetValue(normalized, out var customerId))
        {
            return Task.FromResult<int?>(customerId);
        }

        return Task.FromResult<int?>(null);
    }
}