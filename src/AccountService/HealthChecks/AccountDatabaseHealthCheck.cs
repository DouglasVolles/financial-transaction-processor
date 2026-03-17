using AccountService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AccountService.HealthChecks;

public sealed class AccountDatabaseHealthCheck : IHealthCheck
{
    private readonly AccountDbContext _dbContext;

    public AccountDatabaseHealthCheck(AccountDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Account database is reachable.")
            : HealthCheckResult.Unhealthy("Account database is unreachable.");
    }
}
