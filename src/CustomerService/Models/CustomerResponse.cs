namespace CustomerService.Models;

public class CustomerResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
