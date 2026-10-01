using System.ComponentModel;
using System.Diagnostics;

namespace AsciiVideoPlayer.Tests.Integration;

public sealed class TestVideos : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ascii-video-player-").FullName;

    public static bool FFmpegInstalled { get; } = CanRun("ffmpeg") && CanRun("ffprobe");

    public string Create(string name, params string[] arguments)
    {
        string path = Path.Combine(_directory, name);
        Run("ffmpeg", ["-v", "error", "-y", .. arguments, path]);
        return path;
    }

    public string CreateText(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static void Run(string program, string[] arguments)
    {
        var startInfo = new ProcessStartInfo(program) { RedirectStandardError = true };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        string errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{program} falhou: {errors}");
    }

    private static bool CanRun(string program)
    {
        try
        {
            Run(program, ["-version"]);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
