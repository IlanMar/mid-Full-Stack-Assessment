Transaction Monitor

A .NET 8 and React app for simulating transactions and monitoring them live over SignalR. The latest 200 transactions are kept in memory until the API restarts.

## Run locally

1. Install the .NET 8 SDK and Node.js 22.12 or later.
2. Clone the repository and open its root folder. If you already have a copy, open that folder.

   git clone https://github.com/IlanMar/mid-Full-Stack-Assessment.git
   cd mid-Full-Stack-Assessment

3. Start the API from the repository root.

   dotnet run --project backend/Monitor.Api --urls http://127.0.0.1:5080

4. Keep the API running. In a second terminal at the repository root, install the frontend dependencies and start Vite.

   npm --prefix frontend ci
   npm --prefix frontend run dev

5. Open the [monitor](http://localhost:5173/monitor) in two tabs and wait for **Live**. Click **Send 100** in the [simulator](http://localhost:5173/add). Both tabs update. Use **Errors only** to filter for failed transactions.

## Tests and build

    dotnet test backend/Monitor.Tests
    npm --prefix frontend test
    npm --prefix frontend run build

## API

Example JSON body for `POST /api/transactions`. Use a UTC timestamp ending in `Z` or `+00:00`.

    {
      "transactionId": "e87309df-9b41-4611-9539-f9ec9559e1bd",
      "amount": 1500.50,
      "currency": "USD",
      "status": "Completed",
      "timestamp": "2025-01-01T00:00:00Z"
    }

## Docker and Kubernetes

    docker build -t transaction-monitor:local .
    docker run --rm -p 8080:8080 transaction-monitor:local

Open the [Docker monitor](http://localhost:8080/monitor).

The Kubernetes manifests in `k8s/` use one replica. Scaling to multiple replicas would require a shared transaction store and a SignalR backplane to broadcast events across replicas.
