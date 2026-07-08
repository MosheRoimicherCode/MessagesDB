using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using DatabaseService.Infrastructure.Database;
using Npgsql;

namespace DatabaseService.Infrastructure.Repositories;

public sealed class PostgresMessageRepository : IMessageRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public PostgresMessageRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Message> CreateAsync(Message message)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        const string sql = """
            INSERT INTO messages (
                user_id,
                session_id,
                project_name,
                text,
                created_at_utc
            )
            VALUES (
                @user_id,
                @session_id,
                @project_name,
                @text,
                @created_at_utc
            )
            RETURNING
                id,
                user_id,
                session_id,
                project_name,
                text,
                created_at_utc;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("user_id", message.UserId);
        command.Parameters.AddWithValue("session_id", message.SessionId);
        command.Parameters.AddWithValue("project_name", message.ProjectName);
        command.Parameters.AddWithValue("text", message.Text);
        command.Parameters.AddWithValue("created_at_utc", message.CreatedAtUtc);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Failed to create message.");
        }

        return new Message
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            SessionId = reader.GetString(reader.GetOrdinal("session_id")),
            ProjectName = reader.GetString(reader.GetOrdinal("project_name")),
            Text = reader.GetString(reader.GetOrdinal("text")),
            CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc"))
        };
    }

    public async Task<IReadOnlyList<Message>> GetBySessionIdAsync(string sessionId)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        const string sql = """
        SELECT
            id,
            user_id,
            session_id,
            project_name,
            text,
            created_at_utc
        FROM messages
        WHERE session_id = @session_id
        ORDER BY created_at_utc ASC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);

        await using var reader = await command.ExecuteReaderAsync();

        var messages = new List<Message>();

        while (await reader.ReadAsync())
        {
            messages.Add(new Message
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                SessionId = reader.GetString(reader.GetOrdinal("session_id")),
                ProjectName = reader.GetString(reader.GetOrdinal("project_name")),
                Text = reader.GetString(reader.GetOrdinal("text")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc"))
            });
        }

        return messages;
    }
}