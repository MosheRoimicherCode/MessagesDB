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
    private readonly SemaphoreSlim _clientLimit;

    public TcpServer(
        ClientConnectionHandler handler,
        IPAddress bindAddress,
        int port,
        int maxConcurrentClients)
    {
        _listener = new TcpListener(bindAddress, port);
        _handler = handler;
        _clientLimit = new SemaphoreSlim(maxConcurrentClients);
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
