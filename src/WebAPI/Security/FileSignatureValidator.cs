namespace ShipmentTracking.WebAPI.Security;

public static class FileSignatureValidator
{
    public static async Task<bool> MatchesDeclaredTypeAsync(IFormFile file, CancellationToken ct)
    {
        var signatures = new Dictionary<string, byte[][]>
        {
            ["application/pdf"] = ["%PDF"u8.ToArray()],
            ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
            ["image/png"] = [[0x89, 0x50, 0x4E, 0x47]],
            ["application/msword"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [[0x50, 0x4B, 0x03, 0x04]]
        };
        if (!signatures.TryGetValue(file.ContentType, out var allowed)) return false;

        await using var stream = file.OpenReadStream();
        var bytes = new byte[8];
        var read = await stream.ReadAsync(bytes, ct);
        return allowed.Any(signature => read >= signature.Length && bytes.AsSpan(0, signature.Length).SequenceEqual(signature));
    }
}
