using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Protocol;

public sealed class JsonPacketParser
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public RequestPacket ParseRequest(string json)
    {
        var packet = JsonSerializer.Deserialize<RequestPacket>(json, _options);

        if (packet is null || string.IsNullOrWhiteSpace(packet.Type))
        {
            throw new InvalidOperationException("Invalid request packet.");
        }

        return packet;
    }

    public string SerializeResponse(ResponsePacket response)
    {
        return JsonSerializer.Serialize(response, _options);
    }
}
