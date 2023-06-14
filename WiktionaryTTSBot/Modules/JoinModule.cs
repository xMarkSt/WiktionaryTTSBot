using Discord;
using Discord.Interactions;

using WiktionaryTTSBot.Settings;

namespace WiktionaryTTSBot.Modules;

public class JoinModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MessageListener _messageListener;
    private readonly AudioService _audioService;
    private readonly SettingsService _settingsService;

    public JoinModule(MessageListener messageListener, AudioService audioService, SettingsService settingsService)
    {
        _messageListener = messageListener;
        _audioService = audioService;
        _settingsService = settingsService;
    }
    
    [SlashCommand("setup", "Setup the text channel you're currently in to use for TTS")]
    public async Task Setup()
    {
        ulong guildId = Context.Guild.Id;
        GuildsSettings guildsSettings = await _settingsService.GetGuildsSettings();
        if (guildsSettings.Guilds.TryGetValue(guildId, out GuildSettings? currentGuild))
        {
            currentGuild.TtsChannel = Context.Channel.Id;
        }
        else
        {
            guildsSettings.Guilds.Add(guildId, new GuildSettings {TtsChannel = Context.Channel.Id});
        }
        await _settingsService.SaveGuildsSettings(guildsSettings);
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