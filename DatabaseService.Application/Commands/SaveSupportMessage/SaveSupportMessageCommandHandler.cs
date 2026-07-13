using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using Shared.Protocol;
using System.Text.Json;

namespace DatabaseService.Application.Commands.SaveSupportMessage;

public sealed class SaveSupportMessageCommandHandler : ICommandHandler
{
    private readonly IUserRepository _users;
    private readonly IMessageRepository _messages;

    public SaveSupportMessageCommandHandler(IUserRepository users, IMessageRepository messages)
    {
        _users = users;
        _messages = messages;
    }

    public string CommandType => "SaveSupportMessage";

    public async Task<ResponsePacket> HandleAsync(RequestPacket request)
    {
        if (!string.Equals(request.Type, CommandType, StringComparison.OrdinalIgnoreCase))
        {
            return ResponsePacket.Failure(
                $"Command type incompatible with {nameof(SaveSupportMessageCommandHandler)}"
            );
        }

        if (request.Data is null)
        {
            return ResponsePacket.Failure("Request data is required.");
        }

        SaveSupportMessageData? data;

        try
        {
            data = request.Data.Value.Deserialize<SaveSupportMessageData>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }
        catch (JsonException)
        {
            return ResponsePacket.Failure("Request data is not valid JSON.");
        }

        var missingFields = new List<string>();

        if (data is null)
        {
            return ResponsePacket.Failure("Request data is invalid.");
        }

        if (string.IsNullOrWhiteSpace(data.UserName))
        {
            missingFields.Add(nameof(data.UserName));
        }

        if (string.IsNullOrWhiteSpace(data.PhoneNumber))
        {
            missingFields.Add(nameof(data.PhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(data.SessionId))
        {
            missingFields.Add(nameof(data.SessionId));
        }

        if (string.IsNullOrWhiteSpace(data.ProjectName))
        {
            missingFields.Add(nameof(data.ProjectName));
        }

        if (string.IsNullOrWhiteSpace(data.Text))
        {
            missingFields.Add(nameof(data.Text));
        }

        if (missingFields.Count > 0)
        {
            return ResponsePacket.Failure(
                $"Missing required fields: {string.Join(", ", missingFields)}"
            );
        }

        if (!Enum.TryParse<MessageDirection>(data.Direction, true, out var direction) ||
            !Enum.IsDefined(direction))
        {
            return ResponsePacket.Failure(
                "Direction must be FrontendToTelegram or TelegramToFrontend."
            );
        }

        var user = await _users.GetOrCreateAsync(data.UserName, data.PhoneNumber);

        var message = await _messages.CreateAsync(new Message
        {
            UserId = user.Id,
            SessionId = data.SessionId,
            ProjectName = data.ProjectName,
            Text = data.Text,
            TelegramChatId = data.TelegramChatId,
            TelegramMessageId = data.TelegramMessageId,
            Direction = direction,
            CreatedAtUtc = data.CreatedAtUtc
        });

        return ResponsePacket.Success(new
        {
            messageId = message.Id,
            userId = user.Id
        });
    }
}
