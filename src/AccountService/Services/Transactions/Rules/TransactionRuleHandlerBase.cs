using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public abstract class TransactionRuleHandlerBase : ITransactionRuleHandler
{
    private ITransactionRuleHandler? _next;

    public ITransactionRuleHandler SetNext(ITransactionRuleHandler next)
    {
        _next = next;
        return next;
    }

    public async Task<TransactionRuleResult> HandleAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        if (CanHandle(context.Request.Operation!.Value))
        {
            return await ProcessAsync(context, cancellationToken);
        }

        if (_next is null)
        {
            return TransactionRuleResult.Fail("Unsupported transaction operation");
        }

        return await _next.HandleAsync(context, cancellationToken);
    }

    protected abstract bool CanHandle(TransactionOperation operation);
    protected abstract Task<TransactionRuleResult> ProcessAsync(TransactionRuleContext context, CancellationToken cancellationToken);
}
