namespace CustomerService.Services.CustomerCreation;

public enum CustomerCreationStatus
{
    Created,
    RejectedInvalidData,
    RejectedDuplicate
}

public sealed class CustomerCreationResult
{
    private CustomerCreationResult(CustomerCreationStatus status, string message, int? customerId = null)
    {
        Status = status;
        Message = message;
        CustomerId = customerId;
    }

    public CustomerCreationStatus Status { get; }
    public string Message { get; }
    public int? CustomerId { get; }

    public static CustomerCreationResult Created(int customerId) =>
        new(CustomerCreationStatus.Created, "Customer created", customerId);

    public static CustomerCreationResult RejectedDuplicate(string message) =>
        new(CustomerCreationStatus.RejectedDuplicate, message);
}
