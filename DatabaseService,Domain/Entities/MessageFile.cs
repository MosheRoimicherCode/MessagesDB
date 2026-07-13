namespace DatabaseService.Domain.Entities;

public sealed class MessageFile
{
    public long Id { get; init; }
    public long MessageId { get; init; }
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileKind { get; init; } = string.Empty;
    public MessageFileThumbnail? Thumbnail { get; init; }
}
