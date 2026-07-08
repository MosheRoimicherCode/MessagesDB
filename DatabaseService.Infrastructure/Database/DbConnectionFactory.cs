using Npgsql;

namespace DatabaseService.Infrastructure.Database;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be empty.",
                nameof(connectionString)
            );
        }

        _connectionString = connectionString;
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}