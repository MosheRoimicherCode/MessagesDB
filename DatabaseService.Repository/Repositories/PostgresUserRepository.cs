using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using DatabaseService.Infrastructure.Database;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            INSERT INTO users (name, phone_number)
            VALUES (@name, @phoneNumber)
            RETURNING id, name, phone_number, created_at_utc
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
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                PhoneNumber = reader.GetString(2),
                CreatedAtUtc = reader.GetDateTime(3)
            };
        }

        throw new Exception("Failed to insert or retrieve user.");
    }
}
