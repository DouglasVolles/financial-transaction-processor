using System.Collections.Concurrent;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.Transactions;

public sealed class TransactionProcessor : ITransactionProcessor
{
    private const string ProcessedSuffix = "-PROCESSED";
    private const string AccountNotFoundError = "Account not found";
    private const string UnexpectedProcessingError = "Unexpected error while processing transaction";
    private const decimal CentsDivisor = 100m;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> AccountLocks = new();
    private readonly AccountDbContext _dbContext;
    private readonly ILogger<TransactionProcessor> _logger;

    public TransactionProcessor(AccountDbContext dbContext, ILogger<TransactionProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TransactionResponse> ProcessAsync(TransactionRequest request, CancellationToken cancellationToken)
    {
        var existingResponse = await TryGetExistingTransactionResponseAsync(request.ReferenceId, cancellationToken);
        if (existingResponse != null) return existingResponse;

        var (accountKey, account) = await GetAccountByIdentificationAsync(request.AccountId, cancellationToken);
        if (account == null) return CreateFailedTransactionResponse(request.ReferenceId, AccountNotFoundError);

        var accountLock = AccountLocks.GetOrAdd(accountKey, _ => new SemaphoreSlim(1, 1));
        await accountLock.WaitAsync(cancellationToken);

        try
        {
            return await ProcessWithAccountLockAsync(request, account, accountKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction. ReferenceId: {ReferenceId}", request.ReferenceId);
            return CreateFailedTransactionResponse(request.ReferenceId, UnexpectedProcessingError);
        }
        finally
        {
            accountLock.Release();
        }
    }

    private async Task<TransactionResponse> ProcessWithAccountLockAsync(
        TransactionRequest request,
        Account account,
        string accountKey,
        CancellationToken cancellationToken)
    {
        var existingResponse = await TryGetExistingTransactionResponseAsync(request.ReferenceId, cancellationToken);
        if (existingResponse != null) return existingResponse;

        var transactionEntity = CreateTransactionEntity(request, account, accountKey);

        _dbContext.Transactions.Add(transactionEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateSuccessTransactionResponse(transactionEntity, account);
    }

    private async Task<TransactionResponse?> TryGetExistingTransactionResponseAsync(string referenceId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);

        return existing is null ? null : MapFromEntity(existing);
    }

    private static TransactionResponse MapFromEntity(Transaction entity)
    {
        return new TransactionResponse
        {
            TransactionId = BuildTransactionId(entity.ReferenceId),
            Status = entity.Status,
            Balance = 0,
            ReservedBalance = 0,
            AvailableBalance = 0,
            Timestamp = entity.Timestamp,
            ErrorMessage = entity.ErrorMessage
        };
    }

    private async Task<(string AccountKey, Account? Account)> GetAccountByIdentificationAsync(string accountId, CancellationToken cancellationToken)
    {
        var accountKey = accountId.Trim();
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Identification == accountKey, cancellationToken);

        return (accountKey, account);
    }

    private static TransactionResponse CreateFailedTransactionResponse(string referenceId, string errorMessage)
    {
        return new TransactionResponse
        {
            TransactionId = BuildTransactionId(referenceId),
            Status = TransactionStatus.Failed,
            Balance = 0,
            ReservedBalance = 0,
            AvailableBalance = 0,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };
    }

    private static Transaction CreateTransactionEntity(TransactionRequest request, Account account, string accountKey)
    {
        return new Transaction
        {
            Operation = request.Operation!.Value,
            AccountIdentification = accountKey,
            AccountId = account.Id,
            Amount = request.Amount / CentsDivisor,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            ReferenceId = request.ReferenceId.Trim(),
            Metadata = request.Metadata?.GetRawText() ?? string.Empty,
            Status = TransactionStatus.Success,
            ErrorMessage = null,
            Timestamp = DateTime.UtcNow
        };
    }

    private static TransactionResponse CreateSuccessTransactionResponse(Transaction transactionEntity, Account account)
    {
        return new TransactionResponse
        {
            TransactionId = BuildTransactionId(transactionEntity.ReferenceId),
            Status = transactionEntity.Status,
            Balance = ToCents(account.AvailableBalance + account.ReservedBalance),
            ReservedBalance = ToCents(account.ReservedBalance),
            AvailableBalance = ToCents(account.AvailableBalance),
            Timestamp = transactionEntity.Timestamp,
            ErrorMessage = transactionEntity.ErrorMessage
        };
    }

    private static string BuildTransactionId(string referenceId) => $"{referenceId}{ProcessedSuffix}";

    private static long ToCents(decimal value) => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
}
