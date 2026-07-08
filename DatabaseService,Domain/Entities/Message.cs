namespace DatabaseService.Domain.Entities;

public sealed class Message
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}