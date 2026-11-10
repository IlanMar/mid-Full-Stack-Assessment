using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor;

public interface ITransactionClient
{
    Task TransactionAdded(AcceptedTransaction entry);
}

public sealed class TransactionsHub : Hub<ITransactionClient>;
