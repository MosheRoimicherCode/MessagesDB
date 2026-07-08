using Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Application.Commands;

public sealed class PingCommandHandler : ICommandHandler
{
    public string CommandType => "Ping";
    public Task<ResponsePacket> HandleAsync(RequestPacket request)
    {
        return Task<ResponsePacket>.FromResult(ResponsePacket.Success("pong"));
    }
}
