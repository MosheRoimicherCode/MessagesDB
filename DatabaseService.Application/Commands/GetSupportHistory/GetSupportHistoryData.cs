namespace DatabaseService.Application.Commands.GetSupportHistory;

public sealed class GetSupportHistoryData
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public long AfterMessageId { get; init; }
    public int Limit { get; init; } = 50;
}
