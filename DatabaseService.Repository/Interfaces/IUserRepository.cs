using DatabaseService.Domain.Entities;

namespace DatabaseService.Application.Repositories;

public interface IUserRepository
{
    Task<User> GetOrCreateAsync(string name, string phoneNumber);
}