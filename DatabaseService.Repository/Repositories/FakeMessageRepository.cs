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
            CreatedAtUtc = message.CreatedAtUtc
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
}