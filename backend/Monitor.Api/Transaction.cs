using System.Text.Json.Serialization;

namespace FinancialMonitor;

public sealed record Transaction(
    [property: JsonRequired] Guid TransactionId,
    [property: JsonRequired] decimal Amount,
    [property: JsonRequired] string Currency,
    [property: JsonRequired] string Status,
    [property: JsonRequired, JsonConverter(typeof(UtcTimestampJsonConverter))] DateTimeOffset Timestamp)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (TransactionId == Guid.Empty)
            errors["transactionId"] = ["A non-empty UUID is required."];
        if (Currency is not { Length: 3 } || Currency.Any(c => c is < 'A' or > 'Z'))
            errors["currency"] = ["Use a three-letter uppercase currency code, such as USD."];
        if (Status is not ("Pending" or "Completed" or "Failed"))
            errors["status"] = ["Use Pending, Completed, or Failed."];
        if (Timestamp == default || Timestamp.Offset != TimeSpan.Zero)
            errors["timestamp"] = ["Use a UTC timestamp, such as 2025-01-01T00:00:00Z."];
        return errors;
    }
}

public sealed record AcceptedTransaction(long Sequence, Transaction Transaction);
