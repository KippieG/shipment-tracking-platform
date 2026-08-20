# Operations runbook

## Release

1. Deploy `infra/main.bicep` with secure SQL and JWT parameters.
2. Store connection strings and the JWT secret in Key Vault.
3. Grant the Container App managed identity `Key Vault Secrets User`, `Storage Blob Data Contributor` and `Azure Service Bus Data Sender`.
4. Apply `src/Infrastructure/Persistence/Migrations/20260820120000_InitialCreate.cs` with `dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI` from a .NET 8 environment.
5. Run the manual GitHub Actions deployment with OIDC variables configured.

## Monitoring and recovery

- Alert on `/health` failures, HTTP 5xx rate, `shipment.idempotency.conflicts`, queue dead-letter messages and outbox rows with `AttemptCount >= 10`.
- An outbox record with `LastError` is safe to replay after correcting the dependency; do not manually alter its payload.
- Rotate JWT and connection-string secrets in Key Vault, then restart/revise the Container App.
- Investigate file upload failures using the request trace ID from the ProblemDetails response.
