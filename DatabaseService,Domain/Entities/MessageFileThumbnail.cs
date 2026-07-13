namespace DatabaseService.Domain.Entities;

public sealed class MessageFileThumbnail
{
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
}
