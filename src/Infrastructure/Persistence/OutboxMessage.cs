namespace ShipmentTracking.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload) => new() { Type = type, Payload = payload };

    public void MarkProcessed() { ProcessedAt = DateTime.UtcNow; LastError = null; }
    public void MarkFailed(Exception exception) { AttemptCount++; LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)]; }
}
