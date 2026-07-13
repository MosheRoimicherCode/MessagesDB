namespace DatabaseService.Application.Commands.GetSupportHistory;

public sealed class SupportHistoryPageResponse
{
    public IReadOnlyList<SupportMessageResponse> Messages { get; init; } = [];
    public long NextMessageId { get; init; }
    public bool HasMore { get; init; }
}

public sealed class SupportMessageResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<SupportFileResponse> Files { get; init; } = [];
}

public sealed class SupportFileResponse
{
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
    public SupportFileThumbnailResponse? Thumbnail { get; init; }
}

public sealed class SupportFileThumbnailResponse
{
    public string TelegramFileId { get; init; } = string.Empty;
    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public string PreviewUrl { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
}
