namespace AccountService.Services.Transactions.Rules;

public interface ITransactionRuleEngine
{
    Task<TransactionRuleResult> ApplyAsync(TransactionRuleContext context, CancellationToken cancellationToken);
}
