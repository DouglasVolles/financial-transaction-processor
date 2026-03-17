namespace AccountService.Services.Transactions.Rules;

public interface ITransactionRuleHandler
{
    ITransactionRuleHandler SetNext(ITransactionRuleHandler next);
    Task<TransactionRuleResult> HandleAsync(TransactionRuleContext context, CancellationToken cancellationToken);
}
