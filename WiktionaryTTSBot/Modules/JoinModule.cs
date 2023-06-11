using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace WiktionaryTTSBot.Modules;

public class JoinModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MessageListener _messageListener;
    private readonly AudioService _audioService;
    private readonly IConfiguration _configuration;

    public JoinModule(MessageListener messageListener, AudioService audioService, IConfiguration configuration)
    {
        _messageListener = messageListener;
        _audioService = audioService;
        _configuration = configuration;

    }
    
    [SlashCommand("setup", "Setup the text channel you're currently in to use for TTS")]
    public async Task Setup()
    {
        var guildId = Context.Guild.Id;
        _configuration[$"Guilds:{guildId}:ttsChannel"] = Context.Channel.Id.ToString();

        await RespondAsync("Setup complete. The bot will now listen for messages that are sent in this channel.");
    }

    [SlashCommand("join", "Join the voice channel you're currently in", runMode: RunMode.Async)]
    public async Task Join()
    {
        var guildUser = Context.User as IGuildUser;
        if (guildUser?.VoiceChannel == null)
        {
            await RespondAsync("You're not in a voice channel!");
        }
        else
        {
            await _audioService.JoinAudio(guildUser.Guild, guildUser.VoiceChannel);
            await RespondAsync("Joined the voice channel.");
        }
    }

    [SlashCommand("leave", "Leave the voice channel you're currently in")]
    public async Task Leave()
    {
        if (Context.User is IGuildUser guildUser)
        {
            await _audioService.LeaveAudio(guildUser.Guild);
            await RespondAsync("Left the voice channel");
        }
        else
        {
            await RespondAsync("You're not in a server");
        }
    }
}