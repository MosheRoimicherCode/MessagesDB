using Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Application.Commands;

public interface ICommandHandler
{
    string CommandType { get; }
    Task<ResponsePacket> HandleAsync(RequestPacket request);

}
