using DatabaseService.Api.Tcp;
using DatabaseService.Application.Commands;
using DatabaseService.Application.Commands.GetMessageContextByTelegramReference;
using DatabaseService.Application.Commands.GetSupportHistory;
using DatabaseService.Application.Commands.SaveSupportMessage;
using DatabaseService.Application.Commands.SaveSupportMessageWithFiles;
using DatabaseService.Infrastructure.Database;
using DatabaseService.Infrastructure.Repositories;
using DatabaseService.Repository.Repositories;
using Microsoft.Extensions.Configuration;
using Shared.Protocol;
using System.Net;

namespace MessagesDB;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .Build();

        var connectionString = configuration["ConnectionStrings:MessagesDb"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "MessagesDB connection string is missing. Configure ConnectionStrings:MessagesDb " +
                "in appsettings.json or appsettings.Local.json."
            );
        }

        var bindAddressText = configuration["TcpServer:BindAddress"] ?? "127.0.0.1";
        if (!IPAddress.TryParse(bindAddressText, out var bindAddress))
        {
            throw new InvalidOperationException("TcpServer:BindAddress is invalid.");
        }

        var port = ReadPositiveInt(configuration, "TcpServer:Port", 7700, 65535);
        var maxConcurrentClients = ReadPositiveInt(
            configuration,
            "TcpServer:MaxConcurrentClients",
            100,
            10_000);

        var connectionFactory = new DbConnectionFactory(connectionString);
        var userRepository = new PostgresUserRepository(connectionFactory);
        var messageRepository = new PostgresMessageRepository(connectionFactory);

        var parser = new JsonPacketParser();
        var dispatcher = new CommandDispatcher([
            new PingCommandHandler(),
            new SaveSupportMessageCommandHandler(userRepository, messageRepository),
            new SaveSupportMessageWithFilesCommandHandler(userRepository, messageRepository),
            new GetSupportHistoryCommandHandler(messageRepository),
            new GetMessageContextByTelegramReferenceCommandHandler(messageRepository)
        ]);
        var handler = new ClientConnectionHandler(parser, dispatcher);
        var server = new TcpServer(handler, bindAddress, port, maxConcurrentClients);

        Console.WriteLine(
            $"MessagesDB listening on {bindAddress}:{port}. Max clients: {maxConcurrentClients}."
        );

        await server.StartAsync();
    }

    private static int ReadPositiveInt(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int maximum)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out var parsed) || parsed <= 0 || parsed > maximum)
        {
            throw new InvalidOperationException($"{key} must be between 1 and {maximum}.");
        }

        return parsed;
    }
}
