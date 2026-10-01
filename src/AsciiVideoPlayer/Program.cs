using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
using AsciiVideoPlayer.Playback;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

const int FrameStep = 2;
const int ColumnStep = 2;

string videoPath = @"C:\Users\gusta\Documents\GitHub\video-to-ascii-converter\videoplayback.mp4";

using var video = OpenCvVideoSource.TryOpen(videoPath);

if (video is null)
{
    Console.WriteLine("Error opening the video!");
    return;
}

Console.Title = Path.GetFileNameWithoutExtension(videoPath);

using var audio = OperatingSystem.IsWindows() ? NAudioPlayer.TryOpen(videoPath) : null;

var converter = new AsciiConverter(CharacterPalette.Default, ColumnStep, rowStep: ColumnStep * 2);
var renderer = new TerminalRenderer();
var player = new Player(video, converter, renderer, audio, FrameStep);

player.Play();
