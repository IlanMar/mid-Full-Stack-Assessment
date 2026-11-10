using FinancialMonitor;

namespace Monitor.Tests;

public class TransactionStoreTests
{
    [Fact]
    public void DuplicateDoesNotReplaceTheOriginal()
    {
        var store = new TransactionStore();
        var transaction = Sample();

        var added = store.Add(transaction);
        Assert.NotNull(added);
        Assert.Null(store.Add(transaction with { Amount = 99m }));
        Assert.Equal(added, Assert.Single(store.Snapshot()));
    }

    [Fact]
    public void KeepsTheLatest200InAcceptanceOrder()
    {
        var store = new TransactionStore();
        var entries = Enumerable.Range(0, 205).Select(_ => store.Add(Sample())).ToArray();

        Assert.Equal(entries.Skip(5).Reverse(), store.Snapshot());
    }

    [Fact]
    public async Task HandlesParallelReadsAndWrites()
    {
        var store = new TransactionStore();
        var transactions = Enumerable.Range(0, 100).Select(_ => Sample()).ToArray();
        await Task.WhenAll(transactions.Select(transaction => Task.Run(() =>
        {
            Assert.NotNull(store.Add(transaction));
            var snapshot = store.Snapshot();
            Assert.Equal(snapshot.Length, snapshot.Select(entry => entry.Sequence).Distinct().Count());
        })));

        Assert.Equal(transactions.Select(t => t.TransactionId).Order(),
            store.Snapshot().Select(entry => entry.Transaction.TransactionId).Order());
    }

    [Fact]
    public async Task AcceptsOnlyOneOfSeveralConcurrentDuplicates()
    {
        var store = new TransactionStore();
        var transaction = Sample();
        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => store.Add(transaction))));

        Assert.Single(results.OfType<AcceptedTransaction>());
        Assert.Single(store.Snapshot());
    }

    internal static Transaction Sample() =>
        new(Guid.NewGuid(), 12.50m, "USD", "Completed", DateTimeOffset.UtcNow);
}
