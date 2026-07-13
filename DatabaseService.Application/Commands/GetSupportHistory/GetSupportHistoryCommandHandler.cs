using System.Text.Json;
using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using Shared.Protocol;

namespace DatabaseService.Application.Commands.GetSupportHistory;

public sealed class GetSupportHistoryCommandHandler : ICommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMessageRepository _messages;

    public GetSupportHistoryCommandHandler(IMessageRepository messages)
    {
        _messages = messages;
    }

    public string CommandType => "GetSupportHistory";

    public async Task<ResponsePacket> HandleAsync(RequestPacket request)
    {
        if (!string.Equals(request.Type, CommandType, StringComparison.OrdinalIgnoreCase))
        {
            return ResponsePacket.Failure(
                $"Command type incompatible with {nameof(GetSupportHistoryCommandHandler)}"
            );
        }

        if (request.Data is null)
        {
            return ResponsePacket.Failure("Request data is required.");
        }

        GetSupportHistoryData? data;
        try
        {
            data = request.Data.Value.Deserialize<GetSupportHistoryData>(JsonOptions);
        }
        catch (JsonException)
        {
            return ResponsePacket.Failure("Request data is not valid JSON.");
        }

        if (data is null)
        {
            return ResponsePacket.Failure("Request data is invalid.");
        }

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(data.PhoneNumber)) missingFields.Add(nameof(data.PhoneNumber));
        if (string.IsNullOrWhiteSpace(data.ProjectName)) missingFields.Add(nameof(data.ProjectName));

        if (missingFields.Count > 0)
        {
            return ResponsePacket.Failure(
                $"Missing required fields: {string.Join(", ", missingFields)}"
            );
        }

        if (data.AfterMessageId < 0)
        {
            return ResponsePacket.Failure("AfterMessageId cannot be negative.");
        }

        if (data.Limit is < 1 or > 100)
        {
            return ResponsePacket.Failure("Limit must be between 1 and 100.");
        }

        var history = await _messages.GetHistoryAsync(
            data.PhoneNumber,
            data.ProjectName,
            data.AfterMessageId,
            data.Limit + 1);

        var hasMore = history.Count > data.Limit;
        var pageMessages = history.Take(data.Limit).ToArray();
        var nextMessageId = pageMessages.Length == 0
            ? data.AfterMessageId
            : pageMessages[^1].Id;

        return ResponsePacket.Success(new SupportHistoryPageResponse
        {
            Messages = pageMessages.Select(ToResponse).ToArray(),
            NextMessageId = nextMessageId,
            HasMore = hasMore
        });
    }

    private static SupportMessageResponse ToResponse(Message message)
    {
        return new SupportMessageResponse
        {
            SessionId = message.SessionId,
            Direction = message.Direction.ToString(),
            Text = message.Text,
            CreatedAtUtc = message.CreatedAtUtc,
            Files = message.Files.Select(file => new SupportFileResponse
            {
                TelegramFileId = file.TelegramFileId,
                TelegramFileUniqueId = file.TelegramFileUniqueId,
                FileName = file.FileName,
                MimeType = file.MimeType,
                FileSize = file.FileSize,
                DownloadUrl = string.Empty,
                Thumbnail = file.Thumbnail is null
                    ? null
                    : new SupportFileThumbnailResponse
                    {
                        TelegramFileId = file.Thumbnail.TelegramFileId,
                        TelegramFileUniqueId = file.Thumbnail.TelegramFileUniqueId,
                        PreviewUrl = string.Empty,
                        Width = file.Thumbnail.Width,
                        Height = file.Thumbnail.Height,
                        FileSize = file.Thumbnail.FileSize
                    }
            }).ToArray()
        };
    }
}
