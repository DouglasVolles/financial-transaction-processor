using AccountService.Models;

namespace AccountService.Services.AccountCreation;

public interface IAccountCreationService
{
    Task<AccountCreationResult> CreateAsync(AccountRequest request, CancellationToken cancellationToken);
}

