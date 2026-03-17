using CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerService.HealthChecks;

public sealed class CustomerDatabaseHealthCheck : IHealthCheck
{
    private readonly CustomerDbContext _dbContext;

    public CustomerDatabaseHealthCheck(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Customer database is reachable.")
            : HealthCheckResult.Unhealthy("Customer database is unreachable.");
    }
}
