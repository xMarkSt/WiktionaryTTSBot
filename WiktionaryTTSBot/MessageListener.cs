using System.Diagnostics;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace WiktionaryTTSBot;

public class MessageListener
{
    private readonly DiscordSocketClient _client;
    
    public IAudioClient? AudioClient { get; set; }
    
    public MessageListener(DiscordSocketClient client)
    {
        _client = client;
    }
    
    public void InitializeAsync()
    {
        _client.MessageReceived += OnMessageReceived;
    }
    
    private async Task OnMessageReceived(SocketMessage msg)
    {
        if (msg.Channel.Id == 1117142841358028840 && AudioClient != null)
        {
            Console.WriteLine(msg.ToString());
            await SendAudio(AudioClient,
                "https://upload.wikimedia.org/wikipedia/commons/0/0f/Nl-broodjeaapverhaal.ogg");
        }
    }

    private async Task SendAudio(IAudioClient client, string url)
    {
        Console.WriteLine("Streaming...");
        using Process? ffmpeg = CreateStream(url);
        await using Stream output = ffmpeg.StandardOutput.BaseStream;
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
}