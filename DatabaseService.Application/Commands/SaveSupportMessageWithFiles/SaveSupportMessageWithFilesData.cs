namespace DatabaseService.Application.Commands.SaveSupportMessageWithFiles;

public sealed class SaveSupportMessageWithFilesData
{
    public string SessionId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
    public string Direction { get; init; } = "FrontendToTelegram";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<SaveSupportMessageFileData> Files { get; init; } = [];
}

public sealed class SaveSupportMessageFileData
{
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileKind { get; init; } = string.Empty;
    public SaveSupportMessageFileThumbnailData? Thumbnail { get; init; }
}

public sealed class SaveSupportMessageFileThumbnailData
{
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
}
