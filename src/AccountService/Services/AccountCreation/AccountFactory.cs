using AccountService.Models;

namespace AccountService.Services.AccountCreation;

public class AccountFactory : IAccountFactory
{
    public Account Create(AccountRequest request, string identification)
    {
        return new Account
        {
            Identification = identification,
            AvailableBalance = request.AvailableBalance,
            ReservedBalance = request.ReservedBalance,
            CreditLimit = request.CreditLimit,
            AccountStatus = request.AccountStatus,
        };
    }
}

