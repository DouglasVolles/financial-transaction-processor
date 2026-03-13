using CustomerService.Models;

namespace CustomerService.Services.CustomerCreation;

public class CustomerFactory : ICustomerFactory
{
    public Customer Create(CustomerRequest request, DateTime createdAtUtc)
    {
        return new Customer
        {
            Name = request.Name,
            CpfCnpj = request.CpfCnpj,
            CreatedAt = createdAtUtc
        };
    }
}
