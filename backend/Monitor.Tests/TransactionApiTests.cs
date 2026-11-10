using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Monitor.Tests;

public class TransactionApiTests
{
    [Fact]
    public async Task ValidPostIsStoredAndBroadcastToTwoWebSocketClients()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var first = await Connect(app, timeout.Token);
        using var second = await Connect(app, timeout.Token);
        var id = Guid.NewGuid();

        var invalid = await client.PostAsJsonAsync("/api/transactions",
            Payload(id, "USD", "Unknown"), timeout.Token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/transactions", timeout.Token))!);

        var response = await client.PostAsJsonAsync("/api/transactions",
            Payload(id, "USD", "Completed"), timeout.Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/transactions",
            Payload(id, "USD", "Failed"), timeout.Token);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var entries = (await client.GetFromJsonAsync<JsonElement[]>("/api/transactions", timeout.Token))!;
        var stored = Assert.Single(entries);
        Assert.Equal(id, stored.GetProperty("transaction").GetProperty("transactionId").GetGuid());
        Assert.Equal("Completed", stored.GetProperty("transaction").GetProperty("status").GetString());

        var nextId = Guid.NewGuid();
        var next = await client.PostAsJsonAsync("/api/transactions",
            Payload(nextId, "USD", "Failed"), timeout.Token);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);

        foreach (var socket in new[] { first, second })
        {
            using var message = JsonDocument.Parse(await Receive(socket, timeout.Token));
            Assert.Equal("TransactionAdded", message.RootElement.GetProperty("target").GetString());
            Assert.Equal(stored.GetRawText(),
                message.RootElement.GetProperty("arguments")[0].GetRawText());
            using var nextMessage = JsonDocument.Parse(await Receive(socket, timeout.Token));
            Assert.Equal(nextId, nextMessage.RootElement.GetProperty("arguments")[0]
                .GetProperty("transaction").GetProperty("transactionId").GetGuid());
        }
    }

    [Theory]
    [InlineData("{}", HttpStatusCode.BadRequest)]
    [InlineData("""{"transactionId":"00000000-0000-0000-0000-000000000000","amount":2,"currency":"USD","status":"Pending","timestamp":"2025-01-01T00:00:00Z"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"transactionId":"e87309df-9b41-4611-9539-f9ec9559e1bd","amount":2,"currency":"USD","status":1,"timestamp":"2025-01-01T00:00:00Z"}""", HttpStatusCode.BadRequest)]
    public async Task RejectsInvalidPayload(string json, HttpStatusCode expected)
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/transactions", content);
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("2025-01-01T00:00:00Z", HttpStatusCode.Created)]
    [InlineData("2025-01-01T00:00:00.1234567+00:00", HttpStatusCode.Created)]
    [InlineData("2025-01-01T00:00:00", HttpStatusCode.BadRequest)]
    [InlineData("2025-01-01", HttpStatusCode.BadRequest)]
    [InlineData("2025-01-01T00:00:00+02:00", HttpStatusCode.BadRequest)]
    public async Task RequiresExplicitUtcTimestamp(string timestamp, HttpStatusCode expected)
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/transactions",
            Payload(Guid.NewGuid(), "USD", "Pending", timestamp));
        Assert.Equal(expected, response.StatusCode);
    }

    private static object Payload(Guid id, string currency, string status, string timestamp = "2025-01-01T00:00:00Z") => new
    {
        transactionId = id,
        amount = 12.50m,
        currency,
        status,
        timestamp
    };

    private static async Task<WebSocket> Connect(WebApplicationFactory<Program> app, CancellationToken token)
    {
        var socket = await app.Server.CreateWebSocketClient()
            .ConnectAsync(new Uri("ws://localhost/hubs/transactions"), token);
        var handshake = Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e");
        await socket.SendAsync(handshake.AsMemory(), WebSocketMessageType.Text, true, token);
        Assert.Equal("{}", await Receive(socket, token));
        return socket;
    }

    private static async Task<string> Receive(WebSocket socket, CancellationToken token)
    {
        var buffer = new byte[4096];
        using var message = new MemoryStream();
        ValueWebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer.AsMemory(), token);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            message.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(message.ToArray()).TrimEnd('\u001e');
    }
}
