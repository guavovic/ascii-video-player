using System.Runtime.InteropServices;
using System.Text;

namespace AsciiVideoPlayer.Terminal;

public sealed partial class TerminalSession : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Encoding _previousEncoding = Console.OutputEncoding;

    public TerminalSession()
    {
        if (OperatingSystem.IsWindows())
            EnableVirtualTerminal();

        Console.OutputEncoding = Encoding.UTF8;
        Console.CancelKeyPress += OnCancelKeyPress;
        // Guarda o título da janela e devolve ao sair (22;0t e 23;0t, do xterm); quem não entende ignora.
        Console.Out.Write("\e[22;0t\e[?1049h\e[?25l");
        Console.Out.Flush();
    }

    public CancellationToken Cancellation => _cancellation.Token;

    public void Dispose()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
        Console.Out.Write("\e[0m\e[?25h\e[?1049l\e[23;0t");
        Console.Out.Flush();
        Console.OutputEncoding = _previousEncoding;
        _cancellation.Dispose();
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _cancellation.Cancel();
    }

    private static void EnableVirtualTerminal()
    {
        const int StdOutputHandle = -11;
        const uint EnableVirtualTerminalProcessing = 0x0004;

        nint handle = GetStdHandle(StdOutputHandle);

        if (GetConsoleMode(handle, out uint mode))
            SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int handle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint handle, out uint mode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint handle, uint mode);
}
