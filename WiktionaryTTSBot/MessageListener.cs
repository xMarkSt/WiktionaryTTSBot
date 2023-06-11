using System.Diagnostics;
using Discord.Audio;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace WiktionaryTTSBot;

public class MessageListener
{
    private readonly DiscordSocketClient _client;
    private readonly AudioService _audioService;

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
        ulong zeeland = 1117142841358028840;
        ulong bbr = 1117428599163715735;
        if (msg.Channel.Id == zeeland && msg.Author is SocketGuildUser guildUser &&
            _audioService.IsConnectedToAChannel(guildUser.Guild))
        {
            Console.WriteLine(msg.ToString());
            _ = Task.Run(async () =>
            {
                string word = msg.ToString();
                string lang = "nl";
                string url = $"https://commons.wikimedia.org/wiki/Special:FilePath/{lang}-{word}.ogg";
                await _audioService.SendAudioAsync(guildUser.Guild, msg.Channel, url);
            });
        }
    }
}