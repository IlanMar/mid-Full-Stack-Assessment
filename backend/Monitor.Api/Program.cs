using System.Text.Json.Serialization;
using FinancialMonitor;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TransactionStore>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddSignalR();

var app = builder.Build();

if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseStaticFiles();
    app.MapGet("/", () => Results.Redirect("/monitor"));
    app.MapFallbackToFile("/add", "index.html");
    app.MapFallbackToFile("/monitor", "index.html");
}
app.MapGet("/health", () => Results.Ok());
app.MapGet("/api/transactions", (TransactionStore store) => store.Snapshot());
app.MapPost("/api/transactions", async (
    Transaction transaction,
    TransactionStore store,
    IHubContext<TransactionsHub, ITransactionClient> hub) =>
{
    var errors = transaction.Validate();
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var entry = store.Add(transaction);
    if (entry is null)
        return Results.Conflict(new { error = "This transaction ID is already in the feed." });

    await hub.Clients.All.TransactionAdded(entry);
    return Results.Json(entry, statusCode: StatusCodes.Status201Created);
});
app.MapHub<TransactionsHub>("/hubs/transactions", options =>
    options.Transports = HttpTransportType.WebSockets);

app.Run();

public partial class Program;
