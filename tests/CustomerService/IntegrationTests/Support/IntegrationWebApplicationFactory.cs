using CustomerService.Data;
using CustomerService.Services.Consumers;
using CustomerService.Services.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CustomerService.IntegrationTests.Support;

public sealed class IntegrationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"customer-integration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var hostedServiceDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(CustomerConsumerService));

            if (hostedServiceDescriptor is not null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            services.RemoveAll(typeof(DbContextOptions<CustomerDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CustomerDbContext>));
            services.RemoveAll(typeof(CustomerDbContext));
            services.AddDbContext<CustomerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll(typeof(IRabbitMqService));
            services.RemoveAll(typeof(RabbitMqSettings));
            services.AddSingleton<TestRabbitMqService>();
            services.AddSingleton<IRabbitMqService>(sp => sp.GetRequiredService<TestRabbitMqService>());
        });
    }
}
