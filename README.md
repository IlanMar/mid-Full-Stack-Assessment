Transaction Monitor

A .NET 8 and React app for simulating transactions and watching them arrive live over SignalR. It keeps the latest 200 transactions in memory and clears them on restart.

Run locally

Requires the .NET 8 SDK and Node.js 22.12+. Start the API from the repository root.

sh
dotnet run --project backend/Monitor.Api --urls http://127.0.0.1:5080

Start the frontend in a second terminal, also from the repository root.

sh
npm --prefix frontend ci
npm --prefix frontend run dev

Open two [monitor tabs](http://localhost:5173/monitor) and click **Send 100** in the [simulator](http://localhost:5173/add). Both monitors update live. Use **Errors only** to filter failed transactions.

Tests

sh
dotnet test backend/Monitor.Tests
npm --prefix frontend test
npm --prefix frontend run build

API

Example JSON body for `POST /api/transactions`.

json
{
"transactionId": "e87309df-9b41-4611-9539-f9ec9559e1bd",
"amount": 1500.50,
"currency": "USD",
"status": "Completed",
"timestamp": "2025-01-01T00:00:00Z"
}

The timestamp must end in `Z` or `+00:00`. POST returns `201` on success, `400` for invalid input, or `409` for a duplicate ID in the current history. The monitor loads recent entries via `GET /api/transactions` and listens for `TransactionAdded` on `/hubs/transactions`.

Docker and Kubernetes

sh
docker build -t transaction-monitor:local .
docker run --rm -p 8080:8080 transaction-monitor:local

Open the [container monitor](http://localhost:8080/monitor). For Kubernetes, make the image available to your cluster and apply the manifests.

sh
kubectl apply -f k8s
kubectl port-forward service/transaction-monitor 8080:8080

The deployment uses one replica because history and connections live in one process. Multiple replicas would need a shared store for history and a [SignalR Redis backplane](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-8.0) for live broadcasts.
