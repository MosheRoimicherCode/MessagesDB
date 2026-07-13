namespace DatabaseService.Domain.Entities;

public sealed class Message
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
    public MessageDirection Direction { get; init; } = MessageDirection.FrontendToTelegram;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<MessageFile> Files { get; init; } = [];
}
