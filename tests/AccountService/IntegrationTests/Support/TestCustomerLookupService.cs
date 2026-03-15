using AccountService.Services.CustomerLookup;

namespace AccountService.IntegrationTests.Support;

public sealed class TestCustomerLookupService : ICustomerLookupService
{
    private static readonly IReadOnlyDictionary<string, int> CustomerIdsByCpFCnpj = new Dictionary<string, int>
    {
        ["12345678901"] = 500,
        ["98765432100"] = 901,
        ["11122233344"] = 902
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