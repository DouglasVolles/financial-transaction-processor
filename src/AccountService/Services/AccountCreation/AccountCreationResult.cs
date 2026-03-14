namespace AccountService.Services.AccountCreation;

public enum AccountCreationStatus
{
    Created,
    RejectedInvalidData,
    RejectedDuplicate
}

public sealed class AccountCreationResult
{
    private AccountCreationResult(AccountCreationStatus status, string message, int? accountId = null)
    {
        Status = status;
        Message = message;
        AccountId = accountId;
    }

    public AccountCreationStatus Status { get; }
    public string Message { get; }
    public int? AccountId { get; }

    public static AccountCreationResult Created(int accountId) =>
        new(AccountCreationStatus.Created, "Account created", accountId);

    public static AccountCreationResult RejectedInvalidData(string message) =>
        new(AccountCreationStatus.RejectedInvalidData, message);

    public static AccountCreationResult RejectedDuplicate(string message) =>
        new(AccountCreationStatus.RejectedDuplicate, message);
}

