using AccountService.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountService.UnitTests.Support;

internal static class TestDbContextFactory
{
    public static AccountDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase($"account-tests-{Guid.NewGuid()}")
            .Options;

        return new AccountDbContext(options);
    }
}

