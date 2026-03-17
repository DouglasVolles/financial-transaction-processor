using AccountService.Models;

namespace AccountService.Services.Transactions.Rules;

public sealed class TransactionRuleContext
{
    public required TransactionRequest Request { get; init; }
    public required Account SourceAccount { get; init; }
    public Account? DestinationAccount { get; set; }
    public Transaction? LastTransaction { get; set; }
    public required Transaction TransactionEntity { get; init; }
}
