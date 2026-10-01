namespace AsciiVideoPlayer.Terminal;

public readonly record struct FrameLayout(int Columns, int Rows, int Left, int Top)
{
    private const double CharacterAspectRatio = 2.0;

    public static FrameLayout Fit(int videoWidth, int videoHeight, int terminalColumns, int terminalRows, int? maxWidth)
    {
        double videoAspect = (double)videoWidth / videoHeight;

        int columns = Math.Min(terminalColumns, maxWidth ?? int.MaxValue);
        int rows = (int)Math.Round(columns / videoAspect / CharacterAspectRatio);

        if (rows > terminalRows)
        {
            rows = terminalRows;
            columns = (int)Math.Round(rows * videoAspect * CharacterAspectRatio);
        }

        columns = Math.Max(1, columns);
        rows = Math.Max(1, rows);

        return new FrameLayout(columns, rows, (terminalColumns - columns) / 2, (terminalRows - rows) / 2);
    }
}
