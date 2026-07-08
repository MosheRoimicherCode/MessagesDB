using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;

namespace DatabaseService.Application.Tests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private long _nextId = 1;

    public List<User> Users { get; } = [];

    public Task<User> GetOrCreateAsync(string name, string phoneNumber)
    {
        var existingUser = Users.FirstOrDefault(
            user => user.PhoneNumber == phoneNumber
        );

        if (existingUser is not null)
        {
            return Task.FromResult(existingUser);
        }

        var user = new User
        {
            Id = _nextId++,
            Name = name,
            PhoneNumber = phoneNumber,
            CreatedAtUtc = DateTime.UtcNow
        };

        Users.Add(user);

        return Task.FromResult(user);
    }
}