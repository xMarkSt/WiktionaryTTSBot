using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.Audio;

namespace WiktionaryTTSBot;

public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();

    public async Task JoinAudio(IGuild guild, IVoiceChannel target)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out IAudioClient _))
        {
            return;
        }
        if (target.Guild.Id != guild.Id)
        {
            return;
        }

        var audioClient = await target.ConnectAsync();

        if (_connectedChannels.TryAdd(guild.Id, audioClient))
        {
            //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
        }
    }

    public async Task LeaveAudio(IGuild guild)
    {
        if (_connectedChannels.TryRemove(guild.Id, out IAudioClient? client))
        {
            await client.StopAsync();
            //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
        }
    }
    
    public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
    {
        // Your task: Get a full path to the file if the value of 'path' is only a filename.
        if (!File.Exists(path))
        {
            await channel.SendMessageAsync("File does not exist.");
            return;
        }
        IAudioClient client;
        if (_connectedChannels.TryGetValue(guild.Id, out client))
        {
            //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
            var output = CreateStream(path).StandardOutput.BaseStream;
            var stream = client.CreatePCMStream(AudioApplication.Music);
            await output.CopyToAsync(stream);
            await stream.FlushAsync().ConfigureAwait(false);
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