using DatabaseService.Domain.Entities;

namespace DatabaseService.Application.Repositories;

public interface IMessageRepository
{
    Task<Message> CreateAsync(Message message);

    Task<IReadOnlyList<Message>> GetBySessionIdAsync(string sessionId);
}