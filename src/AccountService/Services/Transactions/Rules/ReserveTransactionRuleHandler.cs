using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class ReserveTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Reserve;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        var amount = context.TransactionEntity.Amount;

        if (amount > context.SourceAccount.AvailableBalance)
        {
            return Task.FromResult(TransactionRuleResult.Fail("Insufficient available balance for reserve."));
        }

        context.SourceAccount.AvailableBalance -= amount;
        context.SourceAccount.ReservedBalance += amount;
        return Task.FromResult(TransactionRuleResult.Success());
    }
}
