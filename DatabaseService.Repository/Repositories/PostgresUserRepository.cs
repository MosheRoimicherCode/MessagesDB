using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using DatabaseService.Infrastructure.Database;
using Npgsql;

namespace DatabaseService.Repository.Repositories;

public class PostgresUserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public PostgresUserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User> GetOrCreateAsync(string name, string phoneNumber)
    {
        const string query = """
            WITH inserted_user AS (
                INSERT INTO users (name, phone_number)
                VALUES (@name, @phoneNumber)
                ON CONFLICT (phone_number) DO NOTHING
                RETURNING id, name, phone_number, created_at_utc
            )
            SELECT id, name, phone_number, created_at_utc
            FROM inserted_user

            UNION ALL

            SELECT id, name, phone_number, created_at_utc
            FROM users
            WHERE phone_number = @phoneNumber

            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("phoneNumber", phoneNumber);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                PhoneNumber = reader.GetString(reader.GetOrdinal("phone_number")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc"))
            };
        }

        throw new InvalidOperationException("Failed to create or retrieve user.");
    }
}
