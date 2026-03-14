using AccountService.Data;
using AccountService.Services.Consumers;
using AccountService.Services.CustomerLookup;
using AccountService.Services.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AccountService.IntegrationTests.Support;

public sealed class IntegrationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"account-integration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var hostedServiceDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(AccountConsumerService));

            if (hostedServiceDescriptor is not null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            services.RemoveAll(typeof(DbContextOptions<AccountDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AccountDbContext>));
            services.RemoveAll(typeof(AccountDbContext));
            services.AddDbContext<AccountDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll(typeof(IRabbitMqService));
            services.RemoveAll(typeof(RabbitMqSettings));
            services.AddSingleton<TestRabbitMqService>();
            services.AddSingleton<IRabbitMqService>(sp => sp.GetRequiredService<TestRabbitMqService>());

            services.RemoveAll(typeof(ICustomerLookupService));
            services.AddSingleton<ICustomerLookupService, TestCustomerLookupService>();
        });
    }
}

