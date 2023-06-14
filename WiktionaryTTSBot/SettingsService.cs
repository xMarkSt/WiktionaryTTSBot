using System.Text.Json;
using WiktionaryTTSBot.Settings;

namespace WiktionaryTTSBot;

public class SettingsService
{
    private const string GuildSettingsFilepath = "guilds.json";
    public async Task<GuildsSettings> GetGuildsSettings()
    {
        var emptySettings = new GuildsSettings { Guilds = new Dictionary<ulong, GuildSettings>() };
        if (!File.Exists(GuildSettingsFilepath))
        {
            return emptySettings;
        }
        try
        {
            await using FileStream openStream = File.OpenRead(GuildSettingsFilepath);
            return await JsonSerializer.DeserializeAsync<GuildsSettings>(openStream) ?? emptySettings;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while retrieving guild settings: {exception}");
        }
        return emptySettings;
    }

    public async Task SaveGuildsSettings(GuildsSettings settings)
    {
        try
        {
            await using FileStream writeStream = File.OpenWrite(GuildSettingsFilepath);
            await JsonSerializer.SerializeAsync(writeStream, settings);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while saving guild settings: {exception}");
        }
    }
}