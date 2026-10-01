using System.Diagnostics;
using AsciiVideoPlayer.Media;
using Silk.NET.OpenAL;

namespace AsciiVideoPlayer.Audio;

public sealed unsafe class OpenAlAudioPlayer : IAudioPlayer
{
    private const int SampleRate = 48_000;
    private const int BytesPerSample = 4;
    private const int ChunkBytes = SampleRate * BytesPerSample / 10;
    private const int ChunkCount = 4;

    private readonly ALContext _alc = ALContext.GetApi(soft: true);
    private readonly AL _al = AL.GetApi(soft: true);
    private readonly Device* _device;
    private readonly Context* _context;
    private readonly uint _source;
    private readonly uint[] _buffers;
    private readonly Dictionary<uint, int> _bufferSizes = [];
    private readonly byte[] _chunk = new byte[ChunkBytes];
    private readonly Process _decoder;
    private readonly Stream _pcm;
    private readonly Lock _lock = new();
    private readonly Stopwatch _sinceEnded = new();
    private Thread? _feeder;
    private volatile bool _stopping;
    private long _playedBytes;
    private TimeSpan _endPosition;

    private OpenAlAudioPlayer(string path)
    {
        _device = _alc.OpenDevice("");

        if (_device is null)
            throw new InvalidOperationException("nenhum dispositivo de áudio disponível.");

        _context = _alc.CreateContext(_device, null);
        _alc.MakeContextCurrent(_context);

        _source = _al.GenSource();
        _buffers = _al.GenBuffers(ChunkCount);

        _decoder = FFmpeg.Start("ffmpeg",
            "-nostdin", "-v", "error",
            "-i", path,
            "-map", "0:a:0",
            "-f", "s16le", "-acodec", "pcm_s16le", "-ac", "2", "-ar", SampleRate.ToString(),
            "pipe:1");

        _pcm = _decoder.StandardOutput.BaseStream;
    }

    public TimeSpan Elapsed
    {
        get
        {
            lock (_lock)
            {
                if (_sinceEnded.IsRunning)
                    return _endPosition + _sinceEnded.Elapsed;

                return PlayedPosition();
            }
        }
    }

    public static OpenAlAudioPlayer? TryOpen(string path)
    {
        try
        {
            return new OpenAlAudioPlayer(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Não foi possível carregar o áudio, o vídeo toca sem som: {ex.Message}");
            return null;
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            foreach (uint buffer in _buffers)
            {
                if (!Fill(buffer))
                    break;

                _al.SourceQueueBuffers(_source, [buffer]);
            }

            _al.SourcePlay(_source);
        }

        _feeder = new Thread(Feed) { IsBackground = true, Name = "audio" };
        _feeder.Start();
    }

    public void Dispose()
    {
        _stopping = true;
        _feeder?.Join();

        if (!_decoder.HasExited)
            _decoder.Kill();

        _decoder.Dispose();

        _al.SourceStop(_source);
        _al.DeleteSource(_source);
        _al.DeleteBuffers(_buffers);

        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);

        _al.Dispose();
        _alc.Dispose();
    }

    private void Feed()
    {
        while (!_stopping)
        {
            lock (_lock)
            {
                RecycleProcessedBuffers();

                _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);

                if (queued == 0)
                {
                    _endPosition = PlayedPosition();
                    _sinceEnded.Start();
                    return;
                }

                _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);

                if ((SourceState)state == SourceState.Stopped)
                    _al.SourcePlay(_source);
            }

            Thread.Sleep(10);
        }
    }

    private void RecycleProcessedBuffers()
    {
        _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);

        for (int i = 0; i < processed; i++)
        {
            uint[] buffer = new uint[1];
            _al.SourceUnqueueBuffers(_source, buffer);
            _playedBytes += _bufferSizes[buffer[0]];

            if (Fill(buffer[0]))
                _al.SourceQueueBuffers(_source, buffer);
        }
    }

    private bool Fill(uint buffer)
    {
        int read = _pcm.ReadAtLeast(_chunk, _chunk.Length, throwOnEndOfStream: false);
        read -= read % BytesPerSample;

        if (read == 0)
            return false;

        fixed (byte* data = _chunk)
            _al.BufferData(buffer, BufferFormat.Stereo16, data, read, SampleRate);

        _bufferSizes[buffer] = read;
        return true;
    }

    private TimeSpan PlayedPosition()
    {
        _al.GetSourceProperty(_source, GetSourceInteger.SampleOffset, out int sampleOffset);
        long bytes = _playedBytes + (long)sampleOffset * BytesPerSample;

        return TimeSpan.FromSeconds((double)bytes / (SampleRate * BytesPerSample));
    }
}
