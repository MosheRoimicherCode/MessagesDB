using DatabaseService.Api.Tcp;
using DatabaseService.Application.Commands;
using DatabaseService.Application.Commands.SaveSupportMessage;
using DatabaseService.Infrastructure.Database;
using DatabaseService.Infrastructure.Repositories;
using DatabaseService.Repository.Repositories;
using Shared.Protocol;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessagesDB;
internal class Program
{
    static async Task Main(string[] args)
    {
        var connectionFactory = new DbConnectionFactory("Host=localhost;Port=5432;Database=messenger;Username=postgres;Password=postgres");

        var userRepository = new PostgresUserRepository(connectionFactory);
        var messageRepository = new PostgresMessageRepository(connectionFactory);

        var saveSupportMessageHandler =
            new SaveSupportMessageCommandHandler(userRepository, messageRepository);

        var parser = new JsonPacketParser(); //Convert entrying packets to Json
        var dispatcher = new CommandDispatcher([ //Dispatch command 
            new PingCommandHandler(), //The list of aceptable commands
            new SaveSupportMessageCommandHandler(userRepository, messageRepository),

        ]);
        var handler = new ClientConnectionHandler(parser, dispatcher); //Responsable to send the action based on requets
        var server = new TcpServer(handler);

        await server.StartAsync();
    }

}
