using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class CaptureTransactionRuleHandler : TransactionRuleHandlerBase
{
    protected override bool CanHandle(TransactionOperation operation) => operation == TransactionOperation.Capture;

    protected override Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        var amount = context.TransactionEntity.Amount;

        if (context.SourceAccount.ReservedBalance != amount)
        {
            return Task.FromResult(TransactionRuleResult.Fail(
                "Capture amount must be equal to the current reserved balance."));
        }
        // Capture settles reserved funds, so available balance remains unchanged.
        context.SourceAccount.ReservedBalance = 0;
        return Task.FromResult(TransactionRuleResult.Success());
    }
}
