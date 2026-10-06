using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AsciiVideoPlayer.Subtitles;

public sealed record SubtitleCue(TimeSpan Start, TimeSpan End, string Text);

public sealed partial class SubtitleTrack(IReadOnlyList<SubtitleCue> cues)
{
    public IReadOnlyList<SubtitleCue> Cues { get; } = cues.OrderBy(cue => cue.Start).ToArray();

    public string? At(TimeSpan time)
    {
        int low = 0, high = Cues.Count - 1, found = -1;

        while (low <= high)
        {
            int middle = (low + high) / 2;

            if (Cues[middle].Start <= time)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        // Legendas podem se sobrepor: procura para trás a mais recente que ainda está na tela.
        for (int i = found; i >= 0 && i > found - 4; i--)
        {
            if (time < Cues[i].End)
                return Cues[i].Text;
        }

        return null;
    }

    public static SubtitleTrack Load(string path) => Parse(Decode(File.ReadAllBytes(path)));

    public static SubtitleTrack Parse(string text)
    {
        var cues = new List<SubtitleCue>();
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var timing = TimingLine().Match(lines[i]);

            if (!timing.Success)
                continue;

            var body = new List<string>();

            for (i++; i < lines.Length && lines[i].Trim().Length > 0; i++)
            {
                string line = Tags().Replace(lines[i], "").Trim();

                if (line.Length > 0)
                    body.Add(line);
            }

            if (body.Count > 0)
                cues.Add(new SubtitleCue(ParseTime(timing.Groups[1].Value), ParseTime(timing.Groups[2].Value), string.Join('\n', body)));
        }

        return new SubtitleTrack(cues);
    }

    // Legenda em português muitas vezes vem em Windows-1252, e não em UTF-8.
    private static string Decode(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('﻿');
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static TimeSpan ParseTime(string text)
    {
        string[] parts = text.Replace(',', '.').Split(':');
        double seconds = 0;

        foreach (string part in parts)
            seconds = seconds * 60 + double.Parse(part, CultureInfo.InvariantCulture);

        return TimeSpan.FromSeconds(seconds);
    }

    [GeneratedRegex(@"((?:\d+:)?\d+:\d+[,.]\d+)\s*-->\s*((?:\d+:)?\d+:\d+[,.]\d+)")]
    private static partial Regex TimingLine();

    // Tags de formatação (<i>, <font color=...>) e de posição ({\an8}) não têm como aparecer no terminal.
    [GeneratedRegex(@"<[^>]*>|\{\\[^}]*\}")]
    private static partial Regex Tags();
}
