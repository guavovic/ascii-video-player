using System.Globalization;

namespace AsciiVideoPlayer.Playback;

public static class TimeFormat
{
    public static string Format(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            time = TimeSpan.Zero;

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : time.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    // Aceita segundos ("90", "12.5") ou minutos e horas com dois-pontos ("1:30", "1:02:03").
    public static bool TryParse(string text, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        string[] parts = text.Split(':');
        double seconds = 0;

        if (parts.Length > 3)
            return false;

        foreach (string part in parts)
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value < 0)
                return false;

            seconds = seconds * 60 + value;
        }

        time = TimeSpan.FromSeconds(seconds);
        return true;
    }
}
