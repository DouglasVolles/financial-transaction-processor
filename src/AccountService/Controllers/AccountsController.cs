using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Controllers;

[ApiController]
[Route("api/financialtransaction/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountDbContext _dbContext;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        AccountDbContext dbContext,
        IRabbitMqService rabbitMqService,
        ILogger<AccountsController> logger)
    {
        _dbContext = dbContext;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    /// <summary>
    /// Add a account request to RabbitMQ queue
    /// </summary>
    /// <param name="accountRequest">Account data: customer id, balances, credit limit and status</param>
    /// <returns>OK if the request was successfully added to the queue</returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public IActionResult AddAccount([FromBody] AccountRequest accountRequest)
    {
        try
        {
            _rabbitMqService.PublishMessage(accountRequest, "accounts");
            _logger.LogInformation("New account request added to queue with status {Status}", accountRequest.AccountStatus);
            return Ok("New account request successfully added to queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding new account to queue");
            return StatusCode(500, "Error adding new account to queue");
        }
    }

    /// <summary>
    /// Get a specific account by ID
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Account details if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetAccount(int id)
    {
        _logger.LogInformation("Getting account by id. AccountId={AccountId}", id);

        var account = await _dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (account == null)
        {
            _logger.LogWarning("Account not found. AccountId={AccountId}", id);
            return NotFound($"Account with ID {id} not found");
        }

        var accountResponse = new AccountResponse
        {
            Id = account.Id,
            CustomerId = account.CustomerId,
            Identification = account.Identification,
            AvailableBalance = account.AvailableBalance,
            ReservedBalance = account.ReservedBalance,
            CreditLimit = account.CreditLimit,
            AccountStatus = account.AccountStatus
        };

        _logger.LogInformation("Account found. AccountId={AccountId}, Identification={Identification}", account.Id, account.Identification);

        return Ok(accountResponse);
    }

    /// <summary>
    /// Get all accounts
    /// </summary>
    /// <returns>List of all accounts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AccountResponse>>> GetAllAccounts()
    {
        _logger.LogInformation("Getting all accounts.");

        var accounts = await _dbContext.Accounts.AsNoTracking().ToListAsync();
        var accountsResponse = accounts.Select(account => new AccountResponse
        {
            Id = account.Id,
            CustomerId = account.CustomerId,
            Identification = account.Identification,
            AvailableBalance = account.AvailableBalance,
            ReservedBalance = account.ReservedBalance,
            CreditLimit = account.CreditLimit,
            AccountStatus = account.AccountStatus
        }).ToList();

        _logger.LogInformation("Accounts retrieved successfully. Count={Count}", accountsResponse.Count);

        return Ok(accountsResponse);
    }
}

