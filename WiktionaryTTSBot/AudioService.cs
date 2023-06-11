using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.Audio;
using Newtonsoft.Json;

namespace WiktionaryTTSBot;

public class AudioInstance
{
    public required IAudioClient Client { get; set; }
    public AudioOutStream? OutStream { get; set; }
}

public class AudioService
{
    /// <summary>
    /// An instance for each Discord Guild (id).
    /// </summary>
    private readonly ConcurrentDictionary<ulong, AudioInstance> _audioInstances = new();

    public async Task JoinAudio(IGuild guild, IVoiceChannel channel)
    {
        // Already in channel?
        if (_audioInstances.TryGetValue(guild.Id, out _))
        {
            return;
        }
        
        // Different guild?
        if (channel.Guild.Id != guild.Id)
        {
            return;
        }

        IAudioClient? audioClient = await channel.ConnectAsync();

        var instance = new AudioInstance
        {
            Client = audioClient
        };
        if (!_audioInstances.TryAdd(guild.Id, instance))
        {
            Console.WriteLine("Error: failed to add AudioInstance");
        }
    }

    public async Task LeaveAudio(IGuild guild)
    {
        if (_audioInstances.TryRemove(guild.Id, out AudioInstance? instance))
        {
            _audioInstances.Remove(guild.Id, out _);
            await instance.Client.StopAsync();
            Console.WriteLine($"Disconnected from voice on guild {guild.Name} ");
            //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
        }
    }

    public bool IsConnectedToAChannel(IGuild guild) => _audioInstances.ContainsKey(guild.Id);

    public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string url)
    {
        //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
        using Process? ffmpeg = CreateStream(url);
        await using Stream output = ffmpeg.StandardOutput.BaseStream;
        if (!_audioInstances.TryGetValue(guild.Id, out AudioInstance? instance))
        {
            Console.WriteLine($"Trying to get audio instance for guild {guild.Name} but failed");
            return;
        }

        instance.OutStream ??= instance.Client.CreatePCMStream(AudioApplication.Mixed);
        
        try
        {
            await output.CopyToAsync(instance.OutStream);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }
        finally
        {
            await instance.OutStream.FlushAsync();
        }
    }

    private Process? CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg.exe",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }
}