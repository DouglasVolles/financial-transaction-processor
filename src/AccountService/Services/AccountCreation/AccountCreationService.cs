using AccountService.Data;
using AccountService.Models;
using AccountService.Services.CustomerLookup;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.AccountCreation;

public class AccountCreationService : IAccountCreationService
{
    private readonly AccountDbContext _dbContext;
    private readonly IAccountFactory _accountFactory;
    private readonly ICustomerLookupService _customerLookupService;

    public AccountCreationService(
        AccountDbContext dbContext,
        IAccountFactory accountFactory,
        ICustomerLookupService customerLookupService)
    {
        _dbContext = dbContext;
        _accountFactory = accountFactory;
        _customerLookupService = customerLookupService;
    }

    public async Task<AccountCreationResult> CreateAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var customerId = await _customerLookupService.FindCustomerIdByCpFCnpjAsync(request.CustomerCpFCnpj, cancellationToken);
        if (!customerId.HasValue)
        {
            return AccountCreationResult.RejectedInvalidData("Customer not found for provided CustomerCpFCnpj");
        }

        var nextIdentification = await GenerateNextIdentificationAsync(cancellationToken);

        var normalizedRequest = new AccountRequest
        {
            CustomerCpFCnpj = request.CustomerCpFCnpj,
            AvailableBalance = request.AvailableBalance,
            ReservedBalance = request.ReservedBalance,
            CreditLimit = request.CreditLimit,
            AccountStatus = request.AccountStatus
        };

        var account = _accountFactory.Create(normalizedRequest, nextIdentification);
        account.CustomerId = customerId.Value;

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return AccountCreationResult.Created(account.Id);
    }

    private async Task<string> GenerateNextIdentificationAsync(CancellationToken cancellationToken)
    {
        var identifications = await _dbContext.Accounts
            .AsNoTracking()
            .Select(a => a.Identification)
            .ToListAsync(cancellationToken);

        var maxNumericPart = identifications
            .Where(static id => !string.IsNullOrWhiteSpace(id) && id.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
            .Select(static id => id[4..])
            .Select(static suffix => int.TryParse(suffix, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        var nextNumber = maxNumericPart + 1;

        return $"ACC-{nextNumber:D3}";
    }
}

