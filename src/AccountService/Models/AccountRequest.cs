namespace AccountService.Models;

public class AccountRequest
{
    public string CustomerCpFCnpj { get; set; } = string.Empty;
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
}

