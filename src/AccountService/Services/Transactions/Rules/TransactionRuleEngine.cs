namespace AccountService.Services.Transactions.Rules;

public sealed class TransactionRuleEngine : ITransactionRuleEngine
{
    private readonly ITransactionRuleHandler _root;

    public TransactionRuleEngine(
        CreditTransactionRuleHandler credit,
        DebitTransactionRuleHandler debit,
        ReserveTransactionRuleHandler reserve,
        CaptureTransactionRuleHandler capture,
        ReversalTransactionRuleHandler reversal,
        TransferTransactionRuleHandler transfer)
    {
        _root = credit;
        _root
            .SetNext(debit)
            .SetNext(reserve)
            .SetNext(capture)
            .SetNext(reversal)
            .SetNext(transfer);
    }

    public Task<TransactionRuleResult> ApplyAsync(TransactionRuleContext context, CancellationToken cancellationToken)
    {
        return _root.HandleAsync(context, cancellationToken);
    }
}
