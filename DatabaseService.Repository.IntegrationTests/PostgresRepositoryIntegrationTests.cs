using DatabaseService.Domain.Entities;
using DatabaseService.Infrastructure.Database;
using DatabaseService.Infrastructure.Repositories;
using DatabaseService.Repository.Repositories;
using Npgsql;

namespace DatabaseService.Repository.IntegrationTests;

public sealed class PostgresRepositoryIntegrationTests
{
    private readonly DbConnectionFactory _connectionFactory = new(GetConnectionString());

    [Fact]
    public async Task GetOrCreateAsync_WhenUserDoesNotExist_CreatesUser()
    {
        await CreateSchemaAsync();

        var users = new PostgresUserRepository(_connectionFactory);
        var phoneNumber = CreateUniquePhoneNumber();

        var user = await users.GetOrCreateAsync("Moshe", phoneNumber);

        Assert.True(user.Id > 0);
        Assert.Equal("Moshe", user.Name);
        Assert.Equal(phoneNumber, user.PhoneNumber);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenUserAlreadyExists_ReturnsExistingUser()
    {
        await CreateSchemaAsync();

        var users = new PostgresUserRepository(_connectionFactory);
        var phoneNumber = CreateUniquePhoneNumber();

        var first = await users.GetOrCreateAsync("Moshe", phoneNumber);
        var second = await users.GetOrCreateAsync("Moshe", phoneNumber);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(phoneNumber, second.PhoneNumber);
    }

    [Fact]
    public async Task MessageRepository_CreateAsync_ThenGetBySessionIdAsync_ReturnsSavedMessage()
    {
        await CreateSchemaAsync();

        var users = new PostgresUserRepository(_connectionFactory);
        var messages = new PostgresMessageRepository(_connectionFactory);

        var user = await users.GetOrCreateAsync("Moshe", CreateUniquePhoneNumber());
        var sessionId = "session-" + Guid.NewGuid().ToString("N");

        var saved = await messages.CreateAsync(new Message
        {
            UserId = user.Id,
            SessionId = sessionId,
            ProjectName = "_5654_NofHagalil_Har_Yona_KIT.fly",
            Text = "test message",
            CreatedAtUtc = DateTime.UtcNow
        });

        var found = await messages.GetBySessionIdAsync(sessionId);

        Assert.True(saved.Id > 0);
        Assert.Single(found);
        Assert.Equal(saved.Id, found[0].Id);
        Assert.Equal(user.Id, found[0].UserId);
        Assert.Equal(sessionId, found[0].SessionId);
        Assert.Equal("test message", found[0].Text);
    }

    private async Task CreateSchemaAsync()
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        var initSqlPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DatabaseService.Infrastructure", "Scripts", "init.sql")
        );

        var sql = await File.ReadAllTextAsync(initSqlPath);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("MESSAGESDB_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set MESSAGESDB_CONNECTION_STRING before running integration tests. " +
                "Example: Host=localhost;Port=5432;Database=messenger;Username=postgres;Password=your_password"
            );
        }

        return connectionString;
    }

    private static string CreateUniquePhoneNumber()
    {
        return "050" + DateTime.UtcNow.Ticks;
    }
}

