using System.Diagnostics;
using System.Runtime.Versioning;
using NAudio.Wave;

namespace AsciiVideoPlayer.Audio;

[SupportedOSPlatform("windows")]
public sealed class NAudioPlayer : IAudioPlayer
{
    private readonly MediaFoundationReader _reader;
    private readonly WaveOutEvent _output = new();
    private readonly Stopwatch _sinceStopped = new();
    private readonly Lock _lock = new();
    private TimeSpan _lastPosition;

    private NAudioPlayer(string path)
    {
        _reader = new MediaFoundationReader(path);
        _output.Init(_reader);
        _output.PlaybackStopped += OnPlaybackStopped;
    }

    public TimeSpan Elapsed
    {
        get
        {
            lock (_lock)
            {
                if (_sinceStopped.IsRunning)
                    return _lastPosition + _sinceStopped.Elapsed;

                var position = TimeSpan.FromSeconds((double)_output.GetPosition() / _output.OutputWaveFormat.AverageBytesPerSecond);

                if (position > _lastPosition)
                    _lastPosition = position;

                return _lastPosition;
            }
        }
    }

    public static NAudioPlayer? TryOpen(string path)
    {
        try
        {
            return new NAudioPlayer(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Não foi possível carregar o áudio, o vídeo toca sem som: {ex.Message}");
            return null;
        }
    }

    public void Start() => _output.Play();

    public void Dispose()
    {
        _output.PlaybackStopped -= OnPlaybackStopped;
        _output.Dispose();
        _reader.Dispose();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
            _sinceStopped.Start();
    }
}
