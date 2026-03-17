using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Controllers;

[ApiController]
[Route("api/financialtransaction/transations")]
public class TransactionController : ControllerBase
{
    private const string TransactionsQueue = "transactions";
    private const string TransactionsExchange = "transactions_exchange";
    private const string TransactionsRoutingKey = "transactions.create";

    private readonly AccountDbContext _dbContext;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        AccountDbContext dbContext,
        IRabbitMqService rabbitMqService,
        ILogger<TransactionController> logger)
    {
        _dbContext = dbContext;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponse>>> Get()
    {
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();

        var response = transactions.Select(transaction => new TransactionResponse
        {
            TransactionId = $"{transaction.ReferenceId}-PROCESSED",
            Status = transaction.Status,
            Balance = ToCents(transaction.AvailableBalance + transaction.ReservedBalance),
            ReservedBalance = ToCents(transaction.ReservedBalance),
            AvailableBalance = ToCents(transaction.AvailableBalance),
            Timestamp = transaction.Timestamp,
            ErrorMessage = transaction.ErrorMessage
        }).ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> AddTransaction([FromBody] TransactionRequest request)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ReferenceId == request.ReferenceId);

        if (transaction is not null)
        {
            return Ok(new TransactionResponse
            {
                TransactionId = $"{transaction.ReferenceId}-PROCESSED",
                Status = transaction.Status,
                Balance = ToCents(transaction.AvailableBalance + transaction.ReservedBalance),
                ReservedBalance = ToCents(transaction.ReservedBalance),
                AvailableBalance = ToCents(transaction.AvailableBalance),
                Timestamp = transaction.Timestamp,
                ErrorMessage = transaction.ErrorMessage
            });
        }

        _rabbitMqService.PublishMessage(
            request,
            queueName: TransactionsQueue,
            exchangeName: TransactionsExchange,
            routingKey: TransactionsRoutingKey);

        _logger.LogInformation("Transaction request queued. ReferenceId: {ReferenceId}", request.ReferenceId);

        return Ok(new TransactionResponse
        {
            TransactionId = $"{request.ReferenceId}-PROCESSED",
            Status = TransactionStatus.Pending,
            Balance = 0,
            ReservedBalance = 0,
            AvailableBalance = 0,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = null
        });
    }

    private static long ToCents(decimal value) => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
}
