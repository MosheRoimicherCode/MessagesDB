using System.Text.Json;
using DatabaseService.Application.Repositories;
using Shared.Protocol;

namespace DatabaseService.Application.Commands.GetMessageContextByTelegramReference;

public sealed class GetMessageContextByTelegramReferenceCommandHandler : ICommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMessageRepository _messages;

    public GetMessageContextByTelegramReferenceCommandHandler(IMessageRepository messages)
    {
        _messages = messages;
    }

    public string CommandType => "GetMessageContextByTelegramReference";

    public async Task<ResponsePacket> HandleAsync(RequestPacket request)
    {
        if (request.Data is null)
        {
            return ResponsePacket.Failure("Request data is required.");
        }

        GetMessageContextByTelegramReferenceData? data;
        try
        {
            data = request.Data.Value.Deserialize<GetMessageContextByTelegramReferenceData>(JsonOptions);
        }
        catch (JsonException)
        {
            return ResponsePacket.Failure("Request data is not valid JSON.");
        }

        if (data is null || data.TelegramChatId == 0 || data.TelegramMessageId <= 0)
        {
            return ResponsePacket.Failure(
                "TelegramChatId and a positive TelegramMessageId are required."
            );
        }

        var context = await _messages.GetContextByTelegramReferenceAsync(
            data.TelegramChatId,
            data.TelegramMessageId);

        return context is null
            ? ResponsePacket.Failure("Original Telegram message context was not found.")
            : ResponsePacket.Success(context);
    }
}
