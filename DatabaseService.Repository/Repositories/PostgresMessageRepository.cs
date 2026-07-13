using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using DatabaseService.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

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
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string messageSql = """
                INSERT INTO messages (
                    user_id,
                    session_id,
                    project_name,
                    text,
                    telegram_chat_id,
                    telegram_message_id,
                    direction,
                    created_at_utc
                )
                VALUES (
                    @user_id,
                    @session_id,
                    @project_name,
                    @text,
                    @telegram_chat_id,
                    @telegram_message_id,
                    @direction,
                    @created_at_utc
                )
                RETURNING id;
                """;

            await using var messageCommand = new NpgsqlCommand(messageSql, connection, transaction);
            messageCommand.Parameters.AddWithValue("user_id", message.UserId);
            messageCommand.Parameters.AddWithValue("session_id", message.SessionId);
            messageCommand.Parameters.AddWithValue("project_name", message.ProjectName);
            messageCommand.Parameters.AddWithValue("text", message.Text);
            messageCommand.Parameters.AddWithValue("telegram_chat_id", message.TelegramChatId);
            messageCommand.Parameters.AddWithValue("telegram_message_id", message.TelegramMessageId);
            messageCommand.Parameters.AddWithValue("direction", (short)message.Direction);
            messageCommand.Parameters.AddWithValue("created_at_utc", message.CreatedAtUtc);

            var idResult = await messageCommand.ExecuteScalarAsync();
            if (idResult is not long messageId)
            {
                throw new InvalidOperationException("Failed to create message.");
            }

            foreach (var file in message.Files)
            {
                await InsertFileAsync(connection, transaction, messageId, file);
            }

            await transaction.CommitAsync();

            return CopyMessage(message, messageId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<Message>> GetBySessionIdAsync(string sessionId)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        var sql = MessageHistorySelect + Environment.NewLine + """
            WHERE m.session_id = @session_id
            ORDER BY m.created_at_utc ASC, m.id ASC, mf.id ASC;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);
        return await ReadMessagesAsync(command);
    }

    public async Task<IReadOnlyList<Message>> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        long afterMessageId = 0,
        int limit = 100)
    {
        if (afterMessageId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(afterMessageId));
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        const string sql = """
            WITH selected_messages AS (
                SELECT m.id
                FROM messages m
                INNER JOIN users u ON u.id = m.user_id
                WHERE u.phone_number = @phone_number
                  AND m.project_name = @project_name
                  AND m.id > @after_message_id
                ORDER BY m.id ASC
                LIMIT @limit
            )
            SELECT
                m.id AS message_id,
                m.user_id,
                m.session_id,
                m.project_name,
                m.text,
                m.telegram_chat_id,
                m.telegram_message_id,
                m.direction,
                m.created_at_utc,
                mf.id AS file_id,
                mf.telegram_file_id,
                mf.telegram_file_unique_id,
                mf.file_name,
                mf.mime_type,
                mf.file_size,
                mf.file_kind,
                mf.thumbnail_telegram_file_id,
                mf.thumbnail_telegram_file_unique_id,
                mf.thumbnail_width,
                mf.thumbnail_height,
                mf.thumbnail_file_size
            FROM selected_messages selected
            INNER JOIN messages m ON m.id = selected.id
            LEFT JOIN message_files mf ON mf.message_id = m.id
            ORDER BY m.id ASC, mf.id ASC;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("phone_number", phoneNumber);
        command.Parameters.AddWithValue("project_name", projectName);
        command.Parameters.AddWithValue("after_message_id", afterMessageId);
        command.Parameters.AddWithValue("limit", limit);
        return await ReadMessagesAsync(command);
    }

    public async Task<MessageContext?> GetContextByTelegramReferenceAsync(
        long telegramChatId,
        long telegramMessageId)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

        const string sql = """
            SELECT
                u.id AS user_id,
                u.name AS user_name,
                u.phone_number,
                m.session_id,
                m.project_name
            FROM messages m
            INNER JOIN users u ON u.id = m.user_id
            WHERE m.telegram_chat_id = @telegram_chat_id
              AND m.telegram_message_id = @telegram_message_id
            ORDER BY m.id DESC
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("telegram_chat_id", telegramChatId);
        command.Parameters.AddWithValue("telegram_message_id", telegramMessageId);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new MessageContext
        {
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            UserName = reader.GetString(reader.GetOrdinal("user_name")),
            PhoneNumber = reader.GetString(reader.GetOrdinal("phone_number")),
            SessionId = reader.GetString(reader.GetOrdinal("session_id")),
            ProjectName = reader.GetString(reader.GetOrdinal("project_name"))
        };
    }

    private static async Task InsertFileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long messageId,
        MessageFile file)
    {
        const string sql = """
            INSERT INTO message_files (
                message_id,
                telegram_file_id,
                telegram_file_unique_id,
                file_name,
                mime_type,
                file_size,
                file_kind,
                thumbnail_telegram_file_id,
                thumbnail_telegram_file_unique_id,
                thumbnail_width,
                thumbnail_height,
                thumbnail_file_size
            )
            VALUES (
                @message_id,
                @telegram_file_id,
                @telegram_file_unique_id,
                @file_name,
                @mime_type,
                @file_size,
                @file_kind,
                @thumbnail_telegram_file_id,
                @thumbnail_telegram_file_unique_id,
                @thumbnail_width,
                @thumbnail_height,
                @thumbnail_file_size
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("telegram_file_id", file.TelegramFileId);
        command.Parameters.AddWithValue("telegram_file_unique_id", file.TelegramFileUniqueId);
        command.Parameters.AddWithValue("file_name", file.FileName);
        command.Parameters.AddWithValue("mime_type", file.MimeType);
        command.Parameters.AddWithValue("file_size", file.FileSize);
        command.Parameters.AddWithValue("file_kind", file.FileKind);
        AddNullableText(command, "thumbnail_telegram_file_id", file.Thumbnail?.TelegramFileId);
        AddNullableText(command, "thumbnail_telegram_file_unique_id", file.Thumbnail?.TelegramFileUniqueId);
        AddNullableInteger(command, "thumbnail_width", file.Thumbnail?.Width);
        AddNullableInteger(command, "thumbnail_height", file.Thumbnail?.Height);
        AddNullableBigint(command, "thumbnail_file_size", file.Thumbnail?.FileSize);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<Message>> ReadMessagesAsync(NpgsqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var builders = new Dictionary<long, MessageBuilder>();

        while (await reader.ReadAsync())
        {
            var messageId = reader.GetInt64(reader.GetOrdinal("message_id"));
            if (!builders.TryGetValue(messageId, out var builder))
            {
                builder = new MessageBuilder
                {
                    Id = messageId,
                    UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                    SessionId = reader.GetString(reader.GetOrdinal("session_id")),
                    ProjectName = reader.GetString(reader.GetOrdinal("project_name")),
                    Text = reader.GetString(reader.GetOrdinal("text")),
                    TelegramChatId = reader.GetInt64(reader.GetOrdinal("telegram_chat_id")),
                    TelegramMessageId = reader.GetInt64(reader.GetOrdinal("telegram_message_id")),
                    Direction = (MessageDirection)reader.GetInt16(reader.GetOrdinal("direction")),
                    CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc"))
                };
                builders.Add(messageId, builder);
            }

            var fileIdOrdinal = reader.GetOrdinal("file_id");
            if (!reader.IsDBNull(fileIdOrdinal))
            {
                builder.Files.Add(ReadFile(reader, messageId));
            }
        }

        return builders.Values.Select(builder => builder.Build()).ToArray();
    }

    private static MessageFile ReadFile(NpgsqlDataReader reader, long messageId)
    {
        var thumbnailFileIdOrdinal = reader.GetOrdinal("thumbnail_telegram_file_id");
        var thumbnail = reader.IsDBNull(thumbnailFileIdOrdinal)
            ? null
            : new MessageFileThumbnail
            {
                TelegramFileId = reader.GetString(thumbnailFileIdOrdinal),
                TelegramFileUniqueId = GetNullableString(reader, "thumbnail_telegram_file_unique_id"),
                Width = GetNullableInt32(reader, "thumbnail_width"),
                Height = GetNullableInt32(reader, "thumbnail_height"),
                FileSize = GetNullableInt64(reader, "thumbnail_file_size")
            };

        return new MessageFile
        {
            Id = reader.GetInt64(reader.GetOrdinal("file_id")),
            MessageId = messageId,
            TelegramFileId = reader.GetString(reader.GetOrdinal("telegram_file_id")),
            TelegramFileUniqueId = reader.GetString(reader.GetOrdinal("telegram_file_unique_id")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            MimeType = reader.GetString(reader.GetOrdinal("mime_type")),
            FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
            FileKind = reader.GetString(reader.GetOrdinal("file_kind")),
            Thumbnail = thumbnail
        };
    }

    private static Message CopyMessage(Message source, long id)
    {
        return new Message
        {
            Id = id,
            UserId = source.UserId,
            SessionId = source.SessionId,
            ProjectName = source.ProjectName,
            Text = source.Text,
            TelegramChatId = source.TelegramChatId,
            TelegramMessageId = source.TelegramMessageId,
            Direction = source.Direction,
            CreatedAtUtc = source.CreatedAtUtc,
            Files = source.Files.Select(file => new MessageFile
            {
                Id = file.Id,
                MessageId = id,
                TelegramFileId = file.TelegramFileId,
                TelegramFileUniqueId = file.TelegramFileUniqueId,
                FileName = file.FileName,
                MimeType = file.MimeType,
                FileSize = file.FileSize,
                FileKind = file.FileKind,
                Thumbnail = file.Thumbnail
            }).ToArray()
        };
    }

    private static void AddNullableText(NpgsqlCommand command, string name, string? value)
    {
        command.Parameters.Add(name, NpgsqlDbType.Text).Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNullableInteger(NpgsqlCommand command, string name, int? value)
    {
        command.Parameters.Add(name, NpgsqlDbType.Integer).Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNullableBigint(NpgsqlCommand command, string name, long? value)
    {
        command.Parameters.Add(name, NpgsqlDbType.Bigint).Value = (object?)value ?? DBNull.Value;
    }

    private static string GetNullableString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static int GetNullableInt32(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static long GetNullableInt64(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt64(ordinal);
    }

    private const string MessageHistorySelect = """
        SELECT
            m.id AS message_id,
            m.user_id,
            m.session_id,
            m.project_name,
            m.text,
            m.telegram_chat_id,
            m.telegram_message_id,
            m.direction,
            m.created_at_utc,
            mf.id AS file_id,
            mf.telegram_file_id,
            mf.telegram_file_unique_id,
            mf.file_name,
            mf.mime_type,
            mf.file_size,
            mf.file_kind,
            mf.thumbnail_telegram_file_id,
            mf.thumbnail_telegram_file_unique_id,
            mf.thumbnail_width,
            mf.thumbnail_height,
            mf.thumbnail_file_size
        FROM messages m
        INNER JOIN users u ON u.id = m.user_id
        LEFT JOIN message_files mf ON mf.message_id = m.id
        """;

    private sealed class MessageBuilder
    {
        public long Id { get; init; }
        public long UserId { get; init; }
        public string SessionId { get; init; } = string.Empty;
        public string ProjectName { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public long TelegramChatId { get; init; }
        public long TelegramMessageId { get; init; }
        public MessageDirection Direction { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public List<MessageFile> Files { get; } = [];

        public Message Build()
        {
            return new Message
            {
                Id = Id,
                UserId = UserId,
                SessionId = SessionId,
                ProjectName = ProjectName,
                Text = Text,
                TelegramChatId = TelegramChatId,
                TelegramMessageId = TelegramMessageId,
                Direction = Direction,
                CreatedAtUtc = CreatedAtUtc,
                Files = Files
            };
        }
    }
}
