using AccountService.Models;

namespace AccountService.Services.AccountCreation;

public interface IAccountFactory
{
    Account Create(AccountRequest request, string identification);
}

