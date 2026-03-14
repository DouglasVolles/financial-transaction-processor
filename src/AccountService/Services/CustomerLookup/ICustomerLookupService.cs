namespace AccountService.Services.CustomerLookup;

public interface ICustomerLookupService
{
    Task<int?> FindCustomerIdByCpFCnpjAsync(string customerCpFCnpj, CancellationToken cancellationToken);
}