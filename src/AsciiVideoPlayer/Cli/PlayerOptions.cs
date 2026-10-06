using AsciiVideoPlayer.Ascii;

namespace AsciiVideoPlayer.Cli;

public sealed record PlayerOptions(
    FileInfo Video,
    int? Width,
    double? Fps,
    string Palette,
    ImageStyle Style,
    int ColorTolerance,
    int ColorSteps,
    bool NoAudio,
    bool NoColor,
    bool Loop,
    FileInfo? Export);
