using Shared.Protocol;

namespace DatabaseService.Application.Commands;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandDispatcher(IEnumerable<ICommandHandler> handlers)
    {
        _handlers = handlers.ToDictionary(
            handler => handler.CommandType,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public Task<ResponsePacket> DispatchAsync(RequestPacket request)
    {
        if (!_handlers.TryGetValue(request.Type, out var handler))
        {
            return Task.FromResult(
                ResponsePacket.Failure($"Unknown command: {request.Type}")
            );
        }

        return handler.HandleAsync(request);
    }
}
