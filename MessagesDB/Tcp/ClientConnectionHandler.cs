using System.Net.Sockets;
using DatabaseService.Application.Commands;
using Shared.Protocol;
namespace DatabaseService.Api.Tcp;

public sealed class ClientConnectionHandler
{
    private readonly JsonPacketParser _parser;
    private readonly CommandDispatcher _dispatcher;

    public ClientConnectionHandler(
        JsonPacketParser parser,
        CommandDispatcher dispatcher)
    {
        _parser = parser;
        _dispatcher = dispatcher;
    }
    public async Task HandleAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        var line = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(line))
        {
            await writer.WriteLineAsync(
                _parser.SerializeResponse(ResponsePacket.Failure("Empty Request")));
            return;
        }
        try
        {
            var request = _parser.ParseRequest(line);

            ResponsePacket response = await _dispatcher.DispatchAsync(request);

            await writer.WriteLineAsync(_parser.SerializeResponse(response));

        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync(
                _parser.SerializeResponse(ResponsePacket.Failure(ex.Message)));
            return;
        }
    }
}
