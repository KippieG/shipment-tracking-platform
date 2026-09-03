# Architecture and reliability model

## Request path

```text
Client → rate limiter → JWT/role policy → idempotency middleware → MediatR handler
      → EF Core transaction (Shipment + OutboxMessage) → SQL Server
                                                 ↓
                                  Outbox worker → Azure Service Bus
                                                 ↓
                                downstream notification/audit consumers
```

The status-command handler adds the state change and the serialized event to the same `ApplicationDbContext`. `SaveChangesAsync` commits them atomically. The worker retries unpublished rows up to ten times and preserves the error for diagnosis; a Service Bus queue then supplies its own dead-letter queue for messages that cannot be delivered.

## Operational contracts

- `POST /api/shipments` accepts an `Idempotency-Key`; equal requests replay the stored successful result and conflicting payloads return `409`.
- Shipment details use cache-aside caching with a five-minute TTL and command-side invalidation. Redis is optional; the cost-conscious Azure profile uses the in-memory implementation.
- `/health` and `/ready` execute database health checks.
- Status-changing endpoints require `Dispatcher`, `Driver` or `Administrator`; creation requires `Dispatcher` or `Administrator`.
- Azure Key Vault is enabled by setting `KeyVault__Uri`; the application uses `DefaultAzureCredential`, so Container Apps can authenticate with its managed identity.

## Deployment model

`infra/main.bicep` declares Azure Container Apps, SQL Database, Key Vault, Application Insights/Log Analytics, Storage and Service Bus. Redis is deliberately omitted from the cost-conscious Azure profile. Store the generated connection strings and JWT secret in Key Vault. Before the first production release, apply the checked-in EF migration and grant the Container App identity access to Key Vault, Blob Storage and Service Bus.
