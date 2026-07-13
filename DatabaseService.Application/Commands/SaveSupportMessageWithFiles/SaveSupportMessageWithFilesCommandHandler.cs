using System.Text.Json;
using DatabaseService.Application.Repositories;
using DatabaseService.Domain.Entities;
using Shared.Protocol;

namespace DatabaseService.Application.Commands.SaveSupportMessageWithFiles;

public sealed class SaveSupportMessageWithFilesCommandHandler : ICommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IUserRepository _users;
    private readonly IMessageRepository _messages;

    public SaveSupportMessageWithFilesCommandHandler(
        IUserRepository users,
        IMessageRepository messages)
    {
        _users = users;
        _messages = messages;
    }

    public string CommandType => "SaveSupportMessageWithFiles";

    public async Task<ResponsePacket> HandleAsync(RequestPacket request)
    {
        if (!string.Equals(request.Type, CommandType, StringComparison.OrdinalIgnoreCase))
        {
            return ResponsePacket.Failure(
                $"Command type incompatible with {nameof(SaveSupportMessageWithFilesCommandHandler)}"
            );
        }

        if (request.Data is null)
        {
            return ResponsePacket.Failure("Request data is required.");
        }

        SaveSupportMessageWithFilesData? data;
        try
        {
            data = request.Data.Value.Deserialize<SaveSupportMessageWithFilesData>(JsonOptions);
        }
        catch (JsonException)
        {
            return ResponsePacket.Failure("Request data is not valid JSON.");
        }

        if (data is null)
        {
            return ResponsePacket.Failure("Request data is invalid.");
        }

        var missingFields = GetMissingFields(data);
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

        var invalidFile = data.Files
            .Select((file, index) => new { file, index })
            .FirstOrDefault(item =>
                string.IsNullOrWhiteSpace(item.file.TelegramFileId) ||
                string.IsNullOrWhiteSpace(item.file.FileKind) ||
                item.file.FileSize < 0);

        if (invalidFile is not null)
        {
            return ResponsePacket.Failure(
                $"Files[{invalidFile.index}] must have TelegramFileId, FileKind, and a non-negative FileSize."
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
            CreatedAtUtc = data.CreatedAtUtc,
            Files = data.Files.Select(ToEntity).ToArray()
        });

        return ResponsePacket.Success(new
        {
            messageId = message.Id,
            userId = user.Id,
            fileCount = message.Files.Count
        });
    }

    private static List<string> GetMissingFields(SaveSupportMessageWithFilesData data)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(data.UserName)) missingFields.Add(nameof(data.UserName));
        if (string.IsNullOrWhiteSpace(data.PhoneNumber)) missingFields.Add(nameof(data.PhoneNumber));
        if (string.IsNullOrWhiteSpace(data.SessionId)) missingFields.Add(nameof(data.SessionId));
        if (string.IsNullOrWhiteSpace(data.ProjectName)) missingFields.Add(nameof(data.ProjectName));
        if (data.Files.Count == 0) missingFields.Add(nameof(data.Files));

        return missingFields;
    }

    private static MessageFile ToEntity(SaveSupportMessageFileData file)
    {
        return new MessageFile
        {
            TelegramFileId = file.TelegramFileId,
            TelegramFileUniqueId = file.TelegramFileUniqueId,
            FileName = file.FileName,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            FileKind = file.FileKind,
            Thumbnail = file.Thumbnail is null
                ? null
                : new MessageFileThumbnail
                {
                    TelegramFileId = file.Thumbnail.TelegramFileId,
                    TelegramFileUniqueId = file.Thumbnail.TelegramFileUniqueId,
                    Width = file.Thumbnail.Width,
                    Height = file.Thumbnail.Height,
                    FileSize = file.Thumbnail.FileSize
                }
        };
    }
}
