using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Api.Tcp;

public sealed class TcpServer
{
    private readonly TcpListener _listener;
    private readonly ClientConnectionHandler _handler;
    private readonly SemaphoreSlim _clientLimit = new(100);
    public  TcpServer(ClientConnectionHandler handler)
    {
        _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7700);
        _handler = handler;
    }

    public async Task StartAsync()
    {
        _listener.Start();

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();

            await _clientLimit.WaitAsync();

            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            await _handler.HandleAsync(client);
        }
        finally
        {
            client.Dispose();
            _clientLimit.Release();
        }
    }
}
