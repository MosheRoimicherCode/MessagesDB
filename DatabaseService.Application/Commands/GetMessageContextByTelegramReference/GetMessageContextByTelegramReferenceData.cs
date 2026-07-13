namespace DatabaseService.Application.Commands.GetMessageContextByTelegramReference;

public sealed class GetMessageContextByTelegramReferenceData
{
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
}
