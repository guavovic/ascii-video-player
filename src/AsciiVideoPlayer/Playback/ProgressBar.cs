namespace AsciiVideoPlayer.Playback;

public static class ProgressBar
{
    public const string Keys = "espaço pausa · ← → 5 s · ↑ ↓ 1 min · Esc sai";

    private const char Played = '━';
    private const char Remaining = '─';

    // Linha do tamanho do terminal: tempo atual, total, barra e, na pausa, as teclas.
    public static string Format(TimeSpan position, TimeSpan duration, bool paused, int width)
    {
        string time = duration > TimeSpan.Zero
            ? $" {TimeFormat.Format(position)} / {TimeFormat.Format(duration)} "
            : $" {TimeFormat.Format(position)} ";

        string suffix = paused ? $" pausado · {Keys} " : " ";
        int barWidth = width - time.Length - suffix.Length;

        if (barWidth < 10)
        {
            suffix = paused ? " pausado " : " ";
            barWidth = width - time.Length - suffix.Length;
        }

        if (duration <= TimeSpan.Zero || barWidth < 1)
            return Fit(time + suffix, width);

        double progress = Math.Clamp(position / duration, 0, 1);
        int played = (int)Math.Round(barWidth * progress);

        return time + new string(Played, played) + new string(Remaining, barWidth - played) + suffix;
    }

    private static string Fit(string text, int width) =>
        text.Length > width ? text[..Math.Max(0, width)] : text.PadRight(width);
}
