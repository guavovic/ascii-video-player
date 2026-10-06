namespace AsciiVideoPlayer.Media;

// Duration fica zero quando o arquivo não informa (por exemplo, uma transmissão ao vivo).
public sealed record MediaInfo(int Width, int Height, double Fps, bool HasAudio, TimeSpan Duration = default);
