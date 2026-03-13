namespace CustomerService.Models;

public class CustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
}
