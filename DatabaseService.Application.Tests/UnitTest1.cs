using DatabaseService.Application.Commands;
using Shared.Protocol;

namespace DatabaseService.Application.Tests;

public class UnitTest1
{
    [Fact]
    public async Task DispatchAsync_WithPingCommand_ReturnsPong()
    {
        var dispatcher = new CommandDispatcher([
            new PingCommandHandler()
        ]);

        var response = await dispatcher.DispatchAsync(new RequestPacket
        {
            Type = "Ping"
        });

        Assert.True(response.Ok);
        Assert.Equal("pong", response.Data);
    }

    [Fact]
    public async Task DispatchAsync_WithUnknownCommand_ReturnsFailure()
    {
        var dispatcher = new CommandDispatcher([
            new PingCommandHandler()
        ]);
        var response = await dispatcher.DispatchAsync(new RequestPacket
        {
            Type = "UnknownCommand"
        });
        Assert.False(response.Ok);
        Assert.Null(response.Data);
        Assert.Equal("Unknown command: UnknownCommand", response.Error);
    }
 
}
