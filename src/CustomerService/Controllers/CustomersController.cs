using CustomerService.Data;
using CustomerService.Models;
using CustomerService.Services.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/financialtransaction/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _dbContext;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        CustomerDbContext dbContext,
        IRabbitMqService rabbitMqService,
        ILogger<CustomersController> logger)
    {
        _dbContext = dbContext;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    /// <summary>
    /// Add a customer request to RabbitMQ queue
    /// </summary>
    /// <param name="customerRequest">Customer data: Name and CpfCnpj</param>
    /// <returns>OK if the request was successfully added to the queue</returns>
    [HttpPost]
    public IActionResult AddCustomer([FromBody] CustomerRequest customerRequest)
    {
        try
        {
            _rabbitMqService.PublishMessage(customerRequest, "customer");
            _logger.LogInformation(
                "New customer request added to queue. Name={Name}, CpfCnpj={CpfCnpj}",
                customerRequest.Name,
                customerRequest.CpfCnpj);
            return Ok("New customer request successfully added to queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding new customer to queue");
            return StatusCode(500, "Error adding new customer to queue");
        }
    }

    /// <summary>
    /// Get a specific customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details if found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
    {
        _logger.LogInformation("Getting customer by id. CustomerId={CustomerId}", id);

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found by id. CustomerId={CustomerId}", id);
            return NotFound($"Customer with ID {id} not found");
        }

        var customerResponse = new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            CpfCnpj = customer.CpfCnpj,
            CreatedAt = customer.CreatedAt
        };

        _logger.LogInformation("Customer found by id. CustomerId={CustomerId}", customer.Id);

        return Ok(customerResponse);
    }

    /// <summary>
    /// Get a specific customer by CpfCnpj
    /// </summary>
    /// <param name="cpfCnpj">Customer CpfCnpj</param>
    /// <returns>Customer details if found</returns>
    [HttpGet("cpfcnpj/{cpfCnpj}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerByCpfCnpj(string cpfCnpj)
    {
        _logger.LogInformation("Getting customer by document. CpfCnpj={CpfCnpj}", cpfCnpj);

        var normalized = NormalizeCpfCnpj(cpfCnpj);
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CpfCnpj == normalized);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found by document. CpfCnpj={CpfCnpj}", cpfCnpj);
            return NotFound($"Customer with CpfCnpj {cpfCnpj} not found");
        }

        var customerResponse = new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            CpfCnpj = customer.CpfCnpj,
            CreatedAt = customer.CreatedAt
        };

        _logger.LogInformation("Customer found by document. CustomerId={CustomerId}", customer.Id);

        return Ok(customerResponse);
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    /// <returns>List of all customers</returns>
    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAllCustomers()
    {
        _logger.LogInformation("Getting all customers.");

        var customers = await _dbContext.Customers.AsNoTracking().ToListAsync();
        var customersResponse = customers.Select(customer => new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            CpfCnpj = customer.CpfCnpj,
            CreatedAt = customer.CreatedAt
        }).ToList();

        _logger.LogInformation("Customers retrieved successfully. Count={Count}", customersResponse.Count);

        return Ok(customersResponse);
    }

    private static string NormalizeCpfCnpj(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
