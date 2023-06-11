using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.Audio;

namespace WiktionaryTTSBot;

public class AudioInstance
{
    public AudioInstance()
    {
        UrlQueue = new Queue<string>();
    }
    public required IAudioClient Client { get; set; }
    public AudioOutStream? OutStream { get; set; }

    public Queue<string> UrlQueue { get; }
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

    public void Enqueue(IGuild guild, string url)
    {
        if (!_audioInstances.TryGetValue(guild.Id, out AudioInstance? instance))
        {
            Console.WriteLine($"Trying to get audio instance for guild {guild.Name} but failed");
            return;
        }
        instance.UrlQueue.Enqueue(url);
    }

    public async Task ProcessQueue(IGuild guild)
    {
        if (!_audioInstances.TryGetValue(guild.Id, out AudioInstance? instance))
        {
            Console.WriteLine($"Trying to get audio instance for guild {guild.Name} but failed");
            return;
        }

        while (instance.UrlQueue.Any())
        {
            string url = instance.UrlQueue.Dequeue();
            await SendAudioAsync(instance, url);
        }
    }

    private async Task SendAudioAsync(AudioInstance instance, string url)
    {
        //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
        using Process? ffmpeg = CreateStream(url);
        if (ffmpeg == null)
        {
            Console.WriteLine("Something went wrong with creating the ffmpeg process.");
            return;
        }
        await using Stream output = ffmpeg.StandardOutput.BaseStream;

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