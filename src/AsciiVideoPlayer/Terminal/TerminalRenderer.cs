using System.Text;

namespace AsciiVideoPlayer.Terminal;

public sealed class TerminalRenderer
{
    private readonly Stream _output = Console.OpenStandardOutput();

    public void Draw(string asciiFrame)
    {
        Console.SetCursorPosition(0, 0);
        _output.Write(Encoding.ASCII.GetBytes(asciiFrame));
    }

    public static bool EscapePressed() =>
        Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape;
}
