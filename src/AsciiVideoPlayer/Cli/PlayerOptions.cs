namespace AsciiVideoPlayer.Cli;

public sealed record PlayerOptions(FileInfo Video, int? Width, double? Fps, string Palette, bool NoAudio, bool NoColor);
