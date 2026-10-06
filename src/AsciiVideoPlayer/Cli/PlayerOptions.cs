using AsciiVideoPlayer.Ascii;

namespace AsciiVideoPlayer.Cli;

public sealed record PlayerOptions(
    string? Video,
    string? Camera,
    int? Width,
    double? Fps,
    string Palette,
    ImageStyle Style,
    int ColorTolerance,
    int ColorSteps,
    bool NoAudio,
    bool NoColor,
    TimeSpan Start,
    FileInfo? Subtitles,
    bool Stats,
    bool Loop,
    FileInfo? Export);
