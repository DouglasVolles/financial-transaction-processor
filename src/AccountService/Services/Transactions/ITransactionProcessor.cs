using AccountService.Models;

namespace AccountService.Services.Transactions;

public interface ITransactionProcessor
{
    Task<TransactionResponse> ProcessAsync(TransactionRequest request, CancellationToken cancellationToken);
}
