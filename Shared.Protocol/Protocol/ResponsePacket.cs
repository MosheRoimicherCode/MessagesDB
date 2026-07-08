using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Shared.Protocol;

public sealed class ResponsePacket
{
    public bool Ok { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }

    public static ResponsePacket Success(object? data = null)
    {
        return new ResponsePacket
        {
            Ok = true,
            Data = data,
        };
    }

    public static ResponsePacket Failure(string error)
    {
        return new ResponsePacket
        {
            Ok = false,
            Error = error
        };
    }
}