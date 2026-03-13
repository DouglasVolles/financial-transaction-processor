using CustomerService.Models;

namespace CustomerService.Services.CustomerCreation;

public interface ICustomerFactory
{
    Customer Create(CustomerRequest request, DateTime createdAtUtc);
}
