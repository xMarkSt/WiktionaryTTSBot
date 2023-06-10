using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace WiktionaryTTSBot;

public static class Commands
{
    public const string JoinCommand = "join";
}

public class Program
{
    private DiscordSocketClient _client = null!;
    private static HttpClient _httpClient = new();

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();

        // Register events
        _client.Log += Log;
        _client.Ready += Client_Ready;
        _client.SlashCommandExecuted += SlashCommandHandler;

        // Get the Discord bot token from environment variable
        string? token = Environment.GetEnvironmentVariable("WIKTIONARYTTSBOT_TOKEN");
        if (token == null)
        {
            Console.WriteLine(
                "Environment variable 'WIKTIONARYTTSBOT_TOKEN' not set! Please set it to your bot token.");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case Commands.JoinCommand:
                var guildUser = command.User as IGuildUser;
                if (guildUser?.VoiceChannel == null)
                {
                    await command.RespondAsync("You're not in a voice channel!");
                }
                else
                {
                    _ = Task.Run(async () =>
                    {        
                        Console.WriteLine("1");
                        IAudioClient? audioClient = await guildUser.VoiceChannel.ConnectAsync();
                        Console.WriteLine("2");
                        await SendAudio(audioClient,
                            "https://upload.wikimedia.org/wikipedia/commons/0/0f/Nl-broodjeaapverhaal.ogg");
                        // Respond to the command
                        Console.WriteLine("3");
                        await command.RespondAsync("Command completed.");
                    });
                }
                break;
        }
    }

    private async Task SendAudio(IAudioClient client, string url)
    {
        // Stream stream = await _httpClient.GetStreamAsync(url);
        Console.WriteLine("Streaming...");
        using var ffmpeg = CreateStream(url);
        using var output = ffmpeg.StandardOutput.BaseStream;
        await using AudioOutStream? discord = client.CreatePCMStream(AudioApplication.Mixed);
        try
        {
            await output.CopyToAsync(discord);
            Console.WriteLine("Copying stream");
        }
        catch (HttpException exception)
        {
            string json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
        }
        finally
        {
            await discord.FlushAsync();
        }
    }
    
    private Process? CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        });
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public async Task Client_Ready()
    {
        // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
        var guild = _client.GetGuild(806546656296042506);

        // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
        var guildCommand = new SlashCommandBuilder();

        // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
        guildCommand.WithName(Commands.JoinCommand);
    
        // Descriptions can have a max length of 100.
        guildCommand.WithDescription("Join the voice channel you're currently in");

        // Let's do our global command
        // var globalCommand = new SlashCommandBuilder();
        // globalCommand.WithName("first-global-command");
        // globalCommand.WithDescription("This is my first global slash command");

        try
        {
            // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            // With global commands we don't need the guild.
            // await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch (HttpException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
        }
    }
}