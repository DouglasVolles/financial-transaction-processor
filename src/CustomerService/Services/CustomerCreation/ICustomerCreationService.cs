using CustomerService.Models;

namespace CustomerService.Services.CustomerCreation;

public interface ICustomerCreationService
{
    Task<CustomerCreationResult> CreateAsync(CustomerRequest request, CancellationToken cancellationToken);
}
