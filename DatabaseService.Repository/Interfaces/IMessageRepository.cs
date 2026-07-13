using DatabaseService.Domain.Entities;

namespace DatabaseService.Application.Repositories;

public interface IMessageRepository
{
    Task<Message> CreateAsync(Message message);

    Task<IReadOnlyList<Message>> GetBySessionIdAsync(string sessionId);

    Task<IReadOnlyList<Message>> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        long afterMessageId = 0,
        int limit = 100);

    Task<MessageContext?> GetContextByTelegramReferenceAsync(
        long telegramChatId,
        long telegramMessageId);
}
