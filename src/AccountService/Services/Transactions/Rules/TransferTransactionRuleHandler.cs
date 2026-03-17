using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class TransferTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Transfer;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        var destinationAccount = context.DestinationAccount;
        if (destinationAccount is null)
        {
            return Task.FromResult(TransactionRuleResult.Fail("Destination account is required for transfer."));
        }

        var amount = context.TransactionEntity.Amount;
        var maxDebitAllowed = context.SourceAccount.AvailableBalance + context.SourceAccount.CreditLimit;

        if (amount > maxDebitAllowed)
        {
            return Task.FromResult(TransactionRuleResult.Fail(
                "Insufficient funds. Available balance plus credit limit is insufficient for transfer."));
        }

        context.SourceAccount.AvailableBalance -= amount;
        destinationAccount.AvailableBalance += amount;
        return Task.FromResult(TransactionRuleResult.Success());
    }
}
