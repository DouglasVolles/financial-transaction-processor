using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class CreditTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Credit;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        context.SourceAccount.AvailableBalance += context.TransactionEntity.Amount;
        return Task.FromResult(TransactionRuleResult.Success());
    }
}
