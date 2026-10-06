using System.Text.RegularExpressions;

namespace AsciiVideoPlayer.Media;

// Só lista os nomes, sem abrir nenhuma câmera: abrir uma às cegas pode ligar uma câmera virtual (a do celular, por exemplo).
public static partial class Cameras
{
    public static IReadOnlyList<string> List()
    {
        if (OperatingSystem.IsLinux())
            return Directory.Exists("/dev") ? Directory.GetFiles("/dev", "video*").Order().ToArray() : [];

        string[] arguments = OperatingSystem.IsWindows()
            ? ["-hide_banner", "-list_devices", "true", "-f", "dshow", "-i", "dummy"]
            : ["-hide_banner", "-list_devices", "true", "-f", "avfoundation", "-i", ""];

        using var process = FFmpeg.Start("ffmpeg", arguments, readErrors: true);
        string output = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return OperatingSystem.IsWindows() ? ParseDirectShow(output) : ParseAvFoundation(output);
    }

    // [dshow] "HD Pro Webcam C920" (video), com "(none)" nas câmeras virtuais e "(audio)" nos microfones.
    public static IReadOnlyList<string> ParseDirectShow(string output) =>
        DirectShowDevice().Matches(output).Select(match => match.Groups[1].Value).ToArray();

    // [AVFoundation indev] [0] FaceTime HD Camera, até a seção de áudio.
    public static IReadOnlyList<string> ParseAvFoundation(string output)
    {
        int audio = output.IndexOf("audio devices", StringComparison.OrdinalIgnoreCase);
        string video = audio >= 0 ? output[..audio] : output;

        return AvFoundationDevice().Matches(video).Select(match => match.Groups[1].Value.Trim()).ToArray();
    }

    [GeneratedRegex(@"""([^""]+)"" \((?:video|none)\)")]
    private static partial Regex DirectShowDevice();

    [GeneratedRegex(@"\] \[\d+\] (.+)")]
    private static partial Regex AvFoundationDevice();
}
