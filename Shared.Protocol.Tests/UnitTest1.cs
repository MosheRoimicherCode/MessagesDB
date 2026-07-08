using DatabaseService.Application.Commands;
using Shared.Protocol;

namespace Shared.Protocol.Tests;

public class UnitTest1
{
    [Fact]
    public void ParseRequest_WithPingJson_ReturnsRequestPacket()
    {
        var parser = new JsonPacketParser();

        var request = parser.ParseRequest("""{"type":"Ping"}""");

        Assert.Equal("Ping", request.Type);
    }

}
