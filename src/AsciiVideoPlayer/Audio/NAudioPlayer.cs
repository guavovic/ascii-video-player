using NAudio.Wave;

namespace AsciiVideoPlayer.Audio;

public sealed class NAudioPlayer : IAudioPlayer
{
    private const int BufferLengthMilliseconds = 4000;

    private readonly MediaFoundationReader _reader;
    private readonly BufferedWaveProvider _buffer;
    private readonly WaveOutEvent _output = new();

    private NAudioPlayer(string path)
    {
        _reader = new MediaFoundationReader(path);
        _buffer = new BufferedWaveProvider(_reader.WaveFormat)
        {
            BufferDuration = TimeSpan.FromMilliseconds(BufferLengthMilliseconds),
            DiscardOnBufferOverflow = true,
            ReadFully = true,
        };

        _output.Init(_buffer);
        _output.Play();
    }

    public static NAudioPlayer? TryOpen(string path)
    {
        try
        {
            return new NAudioPlayer(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar áudio: {ex.Message}");
            return null;
        }
    }

    public void BufferAhead(int seconds)
    {
        byte[] samples = new byte[_reader.WaveFormat.AverageBytesPerSecond * seconds];
        int bytesRead = _reader.Read(samples, 0, samples.Length);

        if (bytesRead > 0)
            _buffer.AddSamples(samples, 0, bytesRead);
    }

    public void Dispose()
    {
        _output.Dispose();
        _reader.Dispose();
    }
}
