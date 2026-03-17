using System.Net.Http.Json;

namespace AccountService.Services.CustomerLookup;

public sealed class CustomerLookupService : ICustomerLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerLookupService> _logger;

    public CustomerLookupService(HttpClient httpClient, ILogger<CustomerLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int?> FindCustomerIdByCpFCnpjAsync(string customerCpFCnpj, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedTarget = NormalizeCpFCnpj(customerCpFCnpj);
            var customers = await _httpClient.GetFromJsonAsync<List<CustomerLookupResponse>>(
                "/api/financialtransaction/customers",
                cancellationToken);

            if (customers is null)
            {
                _logger.LogWarning("Customer lookup returned no data from CustomerService.");
                return null;
            }

            var match = customers.FirstOrDefault(c => NormalizeCpFCnpj(c.CpfCnpj) == normalizedTarget);
            if (match is null)
            {
                _logger.LogInformation("Customer lookup did not find a matching customer.");
            }
            return match?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up customer by CpFCnpj");
            return null;
        }
    }

    private static string NormalizeCpFCnpj(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private sealed class CustomerLookupResponse
    {
        public int Id { get; set; }
        public string CpfCnpj { get; set; } = string.Empty;
    }
}