namespace ShipmentTracking.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Scope { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(string scope, string key, string requestHash, int statusCode, string responseBody) => new()
    {
        Scope = scope,
        Key = key,
        RequestHash = requestHash,
        StatusCode = statusCode,
        ResponseBody = responseBody
    };
}
