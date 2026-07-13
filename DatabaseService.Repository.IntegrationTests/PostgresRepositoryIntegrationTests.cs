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

    [Fact]
    public async Task MessageRepository_CreateWithFile_ThenGetHistory_ReturnsCompleteMetadata()
    {
        await CreateSchemaAsync();

        var users = new PostgresUserRepository(_connectionFactory);
        var messages = new PostgresMessageRepository(_connectionFactory);
        var phoneNumber = CreateUniquePhoneNumber();
        var projectName = "integration-" + Guid.NewGuid().ToString("N") + ".fly";
        var user = await users.GetOrCreateAsync("History user", phoneNumber);

        var saved = await messages.CreateAsync(new Message
        {
            UserId = user.Id,
            SessionId = "session-" + Guid.NewGuid().ToString("N"),
            ProjectName = projectName,
            Text = "message with document",
            TelegramChatId = -100123456,
            TelegramMessageId = 123456,
            Direction = MessageDirection.FrontendToTelegram,
            CreatedAtUtc = DateTime.UtcNow,
            Files =
            [
                new MessageFile
                {
                    TelegramFileId = "telegram-file-id",
                    TelegramFileUniqueId = "telegram-unique-id",
                    FileName = "drawing.pdf",
                    MimeType = "application/pdf",
                    FileSize = 2048,
                    FileKind = "Document",
                    Thumbnail = new MessageFileThumbnail
                    {
                        TelegramFileId = "thumbnail-file-id",
                        TelegramFileUniqueId = "thumbnail-unique-id",
                        Width = 320,
                        Height = 200,
                        FileSize = 512
                    }
                }
            ]
        });

        var history = await messages.GetHistoryAsync(phoneNumber, projectName);

        var message = Assert.Single(history);
        Assert.Equal(saved.Id, message.Id);
        Assert.Equal(123456, message.TelegramMessageId);
        Assert.Equal(MessageDirection.FrontendToTelegram, message.Direction);

        var file = Assert.Single(message.Files);
        Assert.Equal("telegram-file-id", file.TelegramFileId);
        Assert.Equal("drawing.pdf", file.FileName);
        Assert.Equal("Document", file.FileKind);
        Assert.NotNull(file.Thumbnail);
        Assert.Equal("thumbnail-file-id", file.Thumbnail.TelegramFileId);
        Assert.Equal(320, file.Thumbnail.Width);

        var context = await messages.GetContextByTelegramReferenceAsync(
            -100123456,
            123456);

        Assert.NotNull(context);
        Assert.Equal(user.Id, context.UserId);
        Assert.Equal(phoneNumber, context.PhoneNumber);
        Assert.Equal(projectName, context.ProjectName);
        Assert.Equal(saved.SessionId, context.SessionId);

        var nextSaved = await messages.CreateAsync(new Message
        {
            UserId = user.Id,
            SessionId = "session-next-" + Guid.NewGuid().ToString("N"),
            ProjectName = projectName,
            Text = "next message",
            TelegramMessageId = 123457,
            Direction = MessageDirection.TelegramToFrontend,
            CreatedAtUtc = DateTime.UtcNow
        });

        var nextPage = await messages.GetHistoryAsync(
            phoneNumber,
            projectName,
            afterMessageId: saved.Id,
            limit: 1);

        Assert.Equal(nextSaved.Id, Assert.Single(nextPage).Id);
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

