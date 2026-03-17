namespace AccountService.Services.Transactions.Rules;

public sealed class TransactionRuleResult
{
    private TransactionRuleResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    public static TransactionRuleResult Success() => new(true, null);

    public static TransactionRuleResult Fail(string errorMessage) => new(false, errorMessage);
}
