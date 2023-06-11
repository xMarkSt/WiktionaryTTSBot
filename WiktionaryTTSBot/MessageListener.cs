using Discord;
using Discord.WebSocket;

namespace WiktionaryTTSBot;

public class MessageListener
{
    private readonly DiscordSocketClient _client;
    private readonly AudioService _audioService;
    private readonly SemaphoreSlim _audioSemaphore = new(1);

    public MessageListener(DiscordSocketClient client, AudioService audioService)
    {
        _client = client;
        _audioService = audioService;
    }

    public void InitializeAsync()
    {
        _client.MessageReceived += OnMessageReceived;
    }

    private async Task OnMessageReceived(SocketMessage msg)
    {
        ulong zeeland = 1117482626291347498;
        if (msg.Channel.Id == zeeland && msg.Author is SocketGuildUser guildUser &&
            _audioService.IsConnectedToAChannel(guildUser.Guild))
        {
            // This code only allows one message to be processed at a time,
            // but multiple could be processed at the same time as long as it's on different servers.
            // This could be a future optimization
            _ = Task.Run(async () =>
            {
                await _audioSemaphore.WaitAsync();
                try
                {
                    Console.WriteLine(msg.ToString());
                    await ProcessMessage(guildUser.Guild, msg.ToString());
                }
                finally
                {
                    _audioSemaphore.Release();
                }
            });
        }
    }

    private async Task ProcessMessage(IGuild guild, string message)
    {
        const string lang = "nl";
        string[] words = message.Split(' ');
        foreach (string word in words)
        {
            string url = $"https://commons.wikimedia.org/wiki/Special:FilePath/{lang}-{word}.ogg";
            _audioService.Enqueue(guild, url);
        }

        await _audioService.ProcessQueue(guild);
    }
}