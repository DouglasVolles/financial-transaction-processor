using CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomerService.Data;

public class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
        var connectionString = configuration.GetConnectionString("CustomerDatabase");
        optionsBuilder.UseSqlServer(connectionString);

        return new CustomerDbContext(optionsBuilder.Options);
    }
}
