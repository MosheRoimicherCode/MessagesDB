namespace DatabaseService.Domain.Entities;

public sealed class MessageContext
{
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
}
