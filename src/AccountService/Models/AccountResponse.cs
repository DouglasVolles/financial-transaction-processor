namespace AccountService.Models;

public class AccountResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Identification { get; set; } = string.Empty;
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public AccountStatus AccountStatus { get; set; }
}

