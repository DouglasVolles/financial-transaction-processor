using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class ReversalTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Reversal;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        var lastTransaction = context.LastTransaction;
        if (lastTransaction is null)
        {
            return Task.FromResult(TransactionRuleResult.Fail("Reversal requires a previous successful transaction."));
        }

        if (lastTransaction.Amount != context.TransactionEntity.Amount)
        {
            return Task.FromResult(TransactionRuleResult.Fail("Reversal amount must match the previous transaction amount."));
        }

        if (lastTransaction.Operation == TransactionOperation.Debit)
        {
            context.SourceAccount.AvailableBalance += context.TransactionEntity.Amount;
            return Task.FromResult(TransactionRuleResult.Success());
        }

        if (lastTransaction.Operation == TransactionOperation.Credit)
        {
            if (context.SourceAccount.AvailableBalance < context.TransactionEntity.Amount)
            {
                return Task.FromResult(TransactionRuleResult.Fail(
                    "Insufficient available balance to reverse the previous credit transaction."));
            }

            context.SourceAccount.AvailableBalance -= context.TransactionEntity.Amount;
            return Task.FromResult(TransactionRuleResult.Success());
        }

        return Task.FromResult(TransactionRuleResult.Fail(
            "Reversal is only supported for previous debit or credit transactions."));
    }
}
