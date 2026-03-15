using CustomerService.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.UnitTests.Support;

internal static class TestDbContextFactory
{
    public static CustomerDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase($"customer-tests-{Guid.NewGuid()}")
            .Options;

        return new CustomerDbContext(options);
    }
}
