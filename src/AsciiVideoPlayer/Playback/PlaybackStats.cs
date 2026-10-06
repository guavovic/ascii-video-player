using System.Globalization;

namespace AsciiVideoPlayer.Playback;

// Números do último segundo, para o título da janela: atualizar a cada quadro custaria desempenho.
public sealed class PlaybackStats(string name)
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    // Vírgula decimal sem depender de cultura: o binário roda com InvariantGlobalization.
    private static readonly NumberFormatInfo DecimalComma = new() { NumberDecimalSeparator = "," };

    private TimeSpan _since = TimeSpan.MinValue;
    private int _frames;
    private int _skipped;
    private long _bytes;
    private TimeSpan _drawTime;

    public void RecordFrame(int bytes, TimeSpan drawTime)
    {
        _frames++;
        _bytes += bytes;
        _drawTime += drawTime;
    }

    public void RecordSkipped(int frames) => _skipped += frames;

    public string? Report(TimeSpan now)
    {
        if (_since == TimeSpan.MinValue || now < _since)
        {
            Reset(now);
            return null;
        }

        var elapsed = now - _since;

        if (elapsed < Interval || _frames == 0)
            return null;

        string report = string.Create(DecimalComma,
            $"{name} · {_frames / elapsed.TotalSeconds:0.0} fps · {_skipped} pulados · " +
            $"{_bytes / _frames / 1024.0:0.0} KB por quadro · desenho {_drawTime.TotalMilliseconds / _frames:0.00} ms");

        Reset(now);
        return report;
    }

    private void Reset(TimeSpan now)
    {
        _since = now;
        _frames = 0;
        _skipped = 0;
        _bytes = 0;
        _drawTime = TimeSpan.Zero;
    }
}
