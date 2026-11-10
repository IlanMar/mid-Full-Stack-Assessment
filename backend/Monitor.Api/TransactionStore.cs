namespace FinancialMonitor;

public sealed class TransactionStore
{
    private const int Capacity = 200;
    private readonly object gate = new();
    private readonly Queue<AcceptedTransaction> entries = new(Capacity);
    private long sequence;

    public AcceptedTransaction? Add(Transaction transaction)
    {
        lock (gate)
        {
            if (entries.Any(entry => entry.Transaction.TransactionId == transaction.TransactionId))
                return null;

            if (entries.Count == Capacity)
                entries.Dequeue();

            var entry = new AcceptedTransaction(++sequence, transaction);
            entries.Enqueue(entry);
            return entry;
        }
    }

    public AcceptedTransaction[] Snapshot()
    {
        lock (gate)
            return entries.Reverse().ToArray();
    }
}
