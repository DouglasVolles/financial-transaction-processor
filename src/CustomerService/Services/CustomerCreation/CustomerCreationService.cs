using CustomerService.Data;
using CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Services.CustomerCreation;

public class CustomerCreationService : ICustomerCreationService
{
    private readonly CustomerDbContext _dbContext;
    private readonly ICustomerFactory _customerFactory;

    public CustomerCreationService(CustomerDbContext dbContext, ICustomerFactory customerFactory)
    {
        _dbContext = dbContext;
        _customerFactory = customerFactory;
    }

    public async Task<CustomerCreationResult> CreateAsync(CustomerRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = (request.Name ?? string.Empty).Trim();
        var normalizedCpfCnpj = NormalizeCpfCnpj(request.CpfCnpj);
        
        var exists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CpfCnpj == normalizedCpfCnpj, cancellationToken);

        if (exists)
        {
            return CustomerCreationResult.RejectedDuplicate("Customer with same CpfCnpj already exists");
        }

        var normalizedRequest = new CustomerRequest
        {
            Name = normalizedName,
            CpfCnpj = normalizedCpfCnpj
        };

        var customer = _customerFactory.Create(normalizedRequest, DateTime.UtcNow);

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CustomerCreationResult.Created(customer.Id);
    }

    private static string NormalizeCpfCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
