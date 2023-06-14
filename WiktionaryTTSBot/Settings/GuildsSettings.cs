namespace WiktionaryTTSBot.Settings;

public class GuildsSettings
{
    public required Dictionary<ulong, GuildSettings> Guilds { get; set; }
}

public class GuildSettings
{
    public ulong TtsChannel { get; set; }
}