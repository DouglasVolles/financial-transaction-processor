using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountService.UnitTests.Services;

public class TransactionProcessorTests
{
    private static AccountDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase($"transactions-tests-{Guid.NewGuid()}")
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task ProcessAsync_ShouldBeIdempotentByReferenceId()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Accounts.Add(new Account
        {
            CustomerId = 1,
            Identification = "ACC-001",
            AvailableBalance = 100,
            ReservedBalance = 10,
            CreditLimit = 1000,
            AccountStatus = AccountStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var processor = new TransactionProcessor(dbContext, NullLogger<TransactionProcessor>.Instance);
        var request = new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-001",
            Amount = 10000,
            Currency = "BRL",
            ReferenceId = "TXN-001"
        };

        var first = await processor.ProcessAsync(request, CancellationToken.None);
        var second = await processor.ProcessAsync(request, CancellationToken.None);

        first.Status.Should().Be(TransactionStatus.Success);
        second.Status.Should().Be(TransactionStatus.Success);
        second.TransactionId.Should().Be(first.TransactionId);
        dbContext.Transactions.Count().Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_ShouldFailWhenAccountDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var processor = new TransactionProcessor(dbContext, NullLogger<TransactionProcessor>.Instance);

        var result = await processor.ProcessAsync(new TransactionRequest
        {
            Operation = TransactionOperation.Credit,
            AccountId = "ACC-404",
            Amount = 10000,
            Currency = "BRL",
            ReferenceId = "TXN-404"
        }, CancellationToken.None);

        result.Status.Should().Be(TransactionStatus.Failed);
        result.ErrorMessage.Should().Be("Account not found");
        dbContext.Transactions.Count().Should().Be(0);
    }

}
