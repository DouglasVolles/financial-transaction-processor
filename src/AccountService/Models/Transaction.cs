namespace AccountService.Models;

public class Transaction
{
    public int Id { get; set; }
    public TransactionOperation Operation { get; set; }
    public string AccountIdentification { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public string? DestinationAccountIdentification { get; set; }
    public int? DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
