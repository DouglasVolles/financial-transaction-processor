using System.Collections.Concurrent;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Transactions.Rules;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.Transactions;

public sealed class TransactionProcessor : ITransactionProcessor
{
    private const string ProcessedSuffix = "-PROCESSED";
    private const string AccountNotFoundError = "Account not found";
    private const string DestinationAccountRequiredError = "DestinationAccountId is required for transfer operations";
    private const string DestinationAccountNotFoundError = "Destination account not found";
    private const string DestinationAccountSameAsSourceError = "Destination account must be different from source account";
    private const string UnexpectedProcessingError = "Unexpected error while processing transaction";
    private const decimal CentsDivisor = 100m;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> AccountLocks = new();
    private readonly AccountDbContext _dbContext;
    private readonly ITransactionRuleEngine _transactionRuleEngine;
    private readonly ILogger<TransactionProcessor> _logger;

    public TransactionProcessor(
        AccountDbContext dbContext,
        ITransactionRuleEngine transactionRuleEngine,
        ILogger<TransactionProcessor> logger)
    {
        _dbContext = dbContext;
        _transactionRuleEngine = transactionRuleEngine;
        _logger = logger;
    }

    public async Task<TransactionResponse> ProcessAsync(TransactionRequest request, CancellationToken cancellationToken)
    {
        var existingResponse = await TryGetExistingTransactionResponseAsync(request.ReferenceId, cancellationToken);
        if (existingResponse != null) return existingResponse;

        var account = await GetAccountByIdentificationAsync(request.AccountId, cancellationToken);
        if (account == null) return CreateFailedTransactionResponse(request.ReferenceId, AccountNotFoundError);

        var accountLock = AccountLocks.GetOrAdd(account.Identification, _ => new SemaphoreSlim(1, 1));
        await accountLock.WaitAsync(cancellationToken);

        try
        {
            return await ProcessWithAccountLockAsync(request, account, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var existingResponse = await TryGetExistingTransactionResponseAsync(request.ReferenceId, cancellationToken);
        if (existingResponse != null) return existingResponse;       

        var transactionEntity = CreateTransactionEntity(request, account);

        var (ruleContext, preparationError) = await PrepareRuleContextAsync(request, account, transactionEntity, cancellationToken);
        if (preparationError is not null)
        {
            return CreateFailedTransactionResponse(request.ReferenceId, preparationError, account);
        }

        var ruleResult = await _transactionRuleEngine.ApplyAsync(ruleContext!, cancellationToken);
        if (!ruleResult.IsSuccess)
        {
            return CreateFailedTransactionResponse(
                request.ReferenceId,
                ruleResult.ErrorMessage ?? UnexpectedProcessingError,
                account);
        }

        transactionEntity.AvailableBalance = account.AvailableBalance;
        transactionEntity.ReservedBalance = account.ReservedBalance;

        _dbContext.Transactions.Add(transactionEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapFromEntity(transactionEntity);
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
            Balance = ToCents(entity.AvailableBalance + entity.ReservedBalance),
            ReservedBalance = ToCents(entity.ReservedBalance),
            AvailableBalance = ToCents(entity.AvailableBalance),
            Timestamp = entity.Timestamp,
            ErrorMessage = entity.ErrorMessage
        };
    }

    private async Task<Account?> GetAccountByIdentificationAsync(string accountId, CancellationToken cancellationToken)
    {
        var accountIdentification = accountId.Trim();
        return await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Identification == accountIdentification, cancellationToken);
    }

    private static TransactionResponse CreateFailedTransactionResponse(
        string referenceId,
        string errorMessage,
        Account? account = null)
    {
        var availableBalance = account?.AvailableBalance ?? 0m;
        var reservedBalance = account?.ReservedBalance ?? 0m;

        return new TransactionResponse
        {
            TransactionId = BuildTransactionId(referenceId),
            Status = TransactionStatus.Failed,
            Balance = ToCents(availableBalance + reservedBalance),
            ReservedBalance = ToCents(reservedBalance),
            AvailableBalance = ToCents(availableBalance),
            Timestamp = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };
    }

    private static Transaction CreateTransactionEntity(TransactionRequest request, Account account)
    {
        return new Transaction
        {
            Operation = request.Operation!.Value,
            AccountIdentification = account.Identification,
            AccountId = account.Id,
            DestinationAccountIdentification = null,
            DestinationAccountId = null,
            Amount = request.Amount / CentsDivisor,
            AvailableBalance = account.AvailableBalance,
            ReservedBalance = account.ReservedBalance,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            ReferenceId = request.ReferenceId.Trim(),
            Metadata = request.Metadata?.GetRawText() ?? string.Empty,
            Status = TransactionStatus.Success,
            ErrorMessage = null,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<(TransactionRuleContext? Context, string? ErrorMessage)> PrepareRuleContextAsync(
        TransactionRequest request,
        Account sourceAccount,
        Transaction transactionEntity,
        CancellationToken cancellationToken)
    {
        var context = new TransactionRuleContext
        {
            Request = request,
            SourceAccount = sourceAccount,
            TransactionEntity = transactionEntity
        };

        if (request.Operation == TransactionOperation.Transfer)
        {
            var destinationAccountIdentification = request.DestinationAccountId?.Trim();
            if (string.IsNullOrWhiteSpace(destinationAccountIdentification))
            {
                return (null, DestinationAccountRequiredError);
            }

            var destinationAccount = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Identification == destinationAccountIdentification, cancellationToken);

            if (destinationAccount is null)
            {
                return (null, DestinationAccountNotFoundError);
            }

            if (destinationAccount.Identification == sourceAccount.Identification)
            {
                return (null, DestinationAccountSameAsSourceError);
            }

            transactionEntity.DestinationAccountIdentification = destinationAccount.Identification;
            context.DestinationAccount = destinationAccount;
        }

        if (request.Operation == TransactionOperation.Reversal)
        {
            context.LastTransaction = await _dbContext.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == sourceAccount.Id
                            && t.ReferenceId != request.ReferenceId
                            && t.Status == TransactionStatus.Success)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return (context, null);
    }

    private static string BuildTransactionId(string referenceId) => $"{referenceId}{ProcessedSuffix}";

    private static long ToCents(decimal value) => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
}
