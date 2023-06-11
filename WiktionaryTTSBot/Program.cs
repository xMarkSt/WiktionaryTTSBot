using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace WiktionaryTTSBot;

public class Program
{
    private readonly IServiceProvider _services;


    public Program()
    {
        _services = CreateServices();
    }

    static void Main(string[] args)
        => new Program().RunAsync(args).GetAwaiter().GetResult();

    static IServiceProvider CreateServices()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        // X represents either Interaction or Command, as it functions the exact same for both types.
        var servConfig = new InteractionServiceConfig()
        {
            //...
        };

        return new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            // .AddSingleton(servConfig)
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .AddSingleton<MessageListener>()
            .AddSingleton<AudioService>()
            .AddSingleton<HttpClient>()
            .BuildServiceProvider();
    }

    async Task RunAsync(string[] args)
    {
        // Request the instance from the client.
        // Because we're requesting it here first, its targetted constructor will be called and we will receive an active instance.
        var client = _services.GetRequiredService<DiscordSocketClient>();

        client.Log += Log;
        // Get the Discord bot token from environment variable
        string? token = Environment.GetEnvironmentVariable("WIKTIONARYTTSBOT_TOKEN");
        if (token == null)
        {
            Console.WriteLine(
                "Environment variable 'WIKTIONARYTTSBOT_TOKEN' not set! Please set it to your bot token.");
            return;
        }

        // Here we can initialize the service that will register and execute our commands
        await _services.GetRequiredService<InteractionHandler>()
            .InitializeAsync();

        _services.GetRequiredService<MessageListener>()
            .InitializeAsync();

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}