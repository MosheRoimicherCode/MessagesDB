using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Protocol;

public sealed class RequestPacket
{
    public string Type { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
}
