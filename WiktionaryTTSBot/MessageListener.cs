using Discord;
using Discord.WebSocket;
using WiktionaryTTSBot.Settings;

namespace WiktionaryTTSBot;

public class MessageListener
{
    private readonly DiscordSocketClient _client;
    private readonly AudioService _audioService;
    private readonly SettingsService _settingsService;
    private readonly SemaphoreSlim _audioSemaphore = new(1);

    public MessageListener(DiscordSocketClient client, AudioService audioService, SettingsService settingsService)
    {
        _client = client;
        _audioService = audioService;
        _settingsService = settingsService;
    }

    public void InitializeAsync()
    {
        _client.MessageReceived += OnMessageReceived;
    }

    private async Task OnMessageReceived(SocketMessage msg)
    {
        // ulong zeeland = 1117482626291347498;
        GuildsSettings guildsSettings = await _settingsService.GetGuildsSettings();
        if (msg.Author is not SocketGuildUser guildUser) return;
        
        // Bot connected to guild, settings exist and tts channel matches this channel
        if (_audioService.IsConnectedToAChannel(guildUser.Guild) &&
            guildsSettings.Guilds.TryGetValue(guildUser.Guild.Id, out GuildSettings? guildSettings) &&
            msg.Channel.Id == guildSettings.TtsChannel)
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