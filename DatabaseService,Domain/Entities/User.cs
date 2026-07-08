namespace DatabaseService.Domain.Entities;

public sealed class User
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}