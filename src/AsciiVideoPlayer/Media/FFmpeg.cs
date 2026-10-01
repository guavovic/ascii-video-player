using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace AsciiVideoPlayer.Media;

public static class FFmpeg
{
    private const double FallbackFps = 30;

    public static MediaInfo? Probe(string path)
    {
        using var process = Start("ffprobe",
            "-v", "error",
            "-show_entries", "stream=codec_type,width,height,avg_frame_rate,r_frame_rate:stream_side_data=rotation:stream_tags=rotate",
            "-of", "json",
            path);

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            return null;

        using var json = JsonDocument.Parse(output);

        if (!json.RootElement.TryGetProperty("streams", out var streams))
            return null;

        JsonElement? video = null;
        bool hasAudio = false;

        foreach (var stream in streams.EnumerateArray())
        {
            string? type = stream.GetProperty("codec_type").GetString();

            if (type == "video" && video is null)
                video = stream;
            else if (type == "audio")
                hasAudio = true;
        }

        if (video is not { } v || !v.TryGetProperty("width", out var width) || !v.TryGetProperty("height", out var height))
            return null;

        double fps = ParseRate(v, "avg_frame_rate") ?? ParseRate(v, "r_frame_rate") ?? FallbackFps;
        bool rotated = Math.Abs(GetRotation(v)) % 180 == 90;

        return rotated
            ? new MediaInfo(height.GetInt32(), width.GetInt32(), fps, hasAudio)
            : new MediaInfo(width.GetInt32(), height.GetInt32(), fps, hasAudio);
    }

    public static Process Start(string program, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(program)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            var process = Process.Start(startInfo)!;
            process.ErrorDataReceived += (_, _) => { };
            process.BeginErrorReadLine();
            return process;
        }
        catch (Win32Exception ex)
        {
            throw new FFmpegNotFoundException(program, ex);
        }
    }

    public static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double? ParseRate(JsonElement stream, string property)
    {
        if (!stream.TryGetProperty(property, out var value) || value.GetString() is not { } rate)
            return null;

        string[] parts = rate.Split('/');

        if (parts.Length != 2
            || !double.TryParse(parts[0], CultureInfo.InvariantCulture, out double numerator)
            || !double.TryParse(parts[1], CultureInfo.InvariantCulture, out double denominator)
            || numerator <= 0 || denominator <= 0)
            return null;

        return numerator / denominator;
    }

    private static int GetRotation(JsonElement stream)
    {
        if (stream.TryGetProperty("side_data_list", out var sideData))
        {
            foreach (var item in sideData.EnumerateArray())
            {
                if (item.TryGetProperty("rotation", out var rotation) && rotation.TryGetInt32(out int degrees))
                    return degrees;
            }
        }

        if (stream.TryGetProperty("tags", out var tags)
            && tags.TryGetProperty("rotate", out var rotate)
            && int.TryParse(rotate.GetString(), CultureInfo.InvariantCulture, out int tagDegrees))
            return tagDegrees;

        return 0;
    }
}

public sealed class FFmpegNotFoundException(string program, Exception inner)
    : Exception($"Programa '{program}' não encontrado.", inner);
