using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;

namespace DatabaseService.Application.Tests.Fakes;

public sealed class FakeMessageRepository : IMessageRepository
{
    private long _nextId = 1;

    public List<Message> Messages { get; } = [];

    public Task<Message> CreateAsync(Message message)
    {
        var savedMessage = new Message
        {
            Id = _nextId++,
            UserId = message.UserId,
            SessionId = message.SessionId,
            ProjectName = message.ProjectName,
            Text = message.Text,
            TelegramChatId = message.TelegramChatId,
            TelegramMessageId = message.TelegramMessageId,
            Direction = message.Direction,
            CreatedAtUtc = message.CreatedAtUtc,
            Files = message.Files
        };

        Messages.Add(savedMessage);

        return Task.FromResult(savedMessage);
    }

    public Task<IReadOnlyList<Message>> GetBySessionIdAsync(string sessionId)
    {
        return Task.FromResult<IReadOnlyList<Message>>(
            Messages.Where(message => message.SessionId == sessionId).ToList()
        );
    }

    public Task<IReadOnlyList<Message>> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        long afterMessageId = 0,
        int limit = 100)
    {
        return Task.FromResult<IReadOnlyList<Message>>(
            Messages
                .Where(message =>
                    message.ProjectName == projectName &&
                    message.Id > afterMessageId)
                .OrderBy(message => message.Id)
                .Take(limit)
                .ToList()
        );
    }

    public Task<MessageContext?> GetContextByTelegramReferenceAsync(
        long telegramChatId,
        long telegramMessageId)
    {
        var message = Messages.FirstOrDefault(item =>
            item.TelegramChatId == telegramChatId &&
            item.TelegramMessageId == telegramMessageId);

        return Task.FromResult(message is null
            ? null
            : new MessageContext
            {
                UserId = message.UserId,
                SessionId = message.SessionId,
                ProjectName = message.ProjectName
            });
    }
}
