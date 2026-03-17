using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class DebitTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Debit;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        var amount = context.TransactionEntity.Amount;
        var maxDebitAllowed = context.SourceAccount.AvailableBalance + context.SourceAccount.CreditLimit;

        if (amount > maxDebitAllowed)
        {
            return Task.FromResult(TransactionRuleResult.Fail(
                "Insufficient funds. Available balance plus credit limit is insufficient for debit."));
        }

        context.SourceAccount.AvailableBalance -= amount;
        return Task.FromResult(TransactionRuleResult.Success());
    }
}
