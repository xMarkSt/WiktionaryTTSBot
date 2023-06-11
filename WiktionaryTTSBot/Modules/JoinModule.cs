using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace WiktionaryTTSBot.Modules;

public class JoinModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MessageListener _messageListener;
    
    public JoinModule(MessageListener messageListener)
    {
        _messageListener = messageListener;
    }
    
    [SlashCommand("setup", "Setup the text channel you're currently in to use for TTS")]
    public async Task Setup()
    {
        
    }

    [SlashCommand("join", "Join the voice channel you're currently in")]
    public async Task Join()
    {
        var guildUser = Context.User as IGuildUser;
        if (guildUser?.VoiceChannel == null)
        {
            await RespondAsync("You're not in a voice channel!");
        }
        else
        {
            _ = Task.Run(async () =>
            {        
                Console.WriteLine("1");
                _messageListener.AudioClient = await guildUser.VoiceChannel.ConnectAsync();
                Console.WriteLine("2");
                // Respond to the command
                Console.WriteLine("3");
                await RespondAsync("Command completed.");
            });
        }
    }
}