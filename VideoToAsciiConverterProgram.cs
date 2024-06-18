using System.Diagnostics;
using System.Text;
using NAudio.Wave;
using OpenCvSharp;

namespace VideoToAsciiConverter
{
    internal class VideoToAsciiConverterProgram
    {
        private const int FPS_DIVISOR = 2;
        private const int SIZE_DIVISOR = 2;
        private const int AUDIO_BUFFER_LENGHT = 1000;

        private static readonly Stream ConsoleStream = Console.OpenStandardOutput();

        private static VideoCapture Capture = new VideoCapture();
        private static int FrameWidth;
        private static int FrameHeight;
        private static int HeightDivisor;
        private static int FrameDelay;

        private static MediaFoundationReader reader;
        private static BufferedWaveProvider bufferedWaveProvider;
        private static WaveOutEvent player;
        private static bool HasAudio;

        static void Main()
        {
            string videoPath = @"C:\Users\gusta\Documents\GitHub\video-to-ascii-converter\videoplayback.mp4";
            Capture = new VideoCapture(videoPath, VideoCaptureAPIs.ANY);

            if (!Capture.IsOpened())
            {
                Console.WriteLine("Error opening the video!");
                return;
            }

            InitializeConsole(videoPath);
            InitializeAudio(videoPath); // Arrumar audio 

            ProcessVideo();
        }

        private static void InitializeConsole(string videoPath)
        {
            FrameWidth = Capture.FrameWidth;
            FrameHeight = Capture.FrameHeight;
            HeightDivisor = SIZE_DIVISOR * 2;
            FrameDelay = (int)(1000 / ((int)Capture.Fps / FPS_DIVISOR));

            // Console.SetWindowSize(FrameWidth / SIZE_DIVISOR, FrameHeight / HeightDivisor); // Arrumar tela de output
            Console.Title = Path.GetFileNameWithoutExtension(videoPath);
        }

        private static void InitializeAudio(string videoPath)
        {
            try
            {
                using (var reader = new MediaFoundationReader(videoPath))
                {
                    bufferedWaveProvider = new BufferedWaveProvider(reader.WaveFormat);
                    bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(AUDIO_BUFFER_LENGHT * 4);
                    bufferedWaveProvider.DiscardOnBufferOverflow = true;
                    bufferedWaveProvider.ReadFully = true;

                    player = new WaveOutEvent();
                    player.Init(bufferedWaveProvider);
                    HasAudio = true;
                }
            }
            catch (Exception ex)
            {
                HasAudio = false;
                Console.WriteLine($"Erro ao carregar áudio: {ex.Message}");
            }
        }

        private static void ProcessVideo()
        {
            var stopwatch = new Stopwatch();
            var stringBuilder = new StringBuilder();
            var frame = new Mat();
            int currentSecond = 0;

            while (true)
            {
                Capture.Read(frame);

                if (frame.Empty())
                    break;

                ClearConsole();
                SkipFrames();

                if (HasAudio)
                {
                    WriteAudio(currentSecond == 0 ? 2 : 1);
                    currentSecond++;
                }

                SkipFrames();
                WriteToConsole(ConvertFrameToAscii(frame, stringBuilder));
                AdjustSleepTime(FrameDelay, stopwatch);

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    break;
            }
        }

        private static void SkipFrames()
        {
            for (int i = 0; i < FPS_DIVISOR - 1; i++)
                Capture.Grab();
        }

        private static string ConvertFrameToAscii(Mat frame, StringBuilder sb)
        {
            for (int y = 0; y < FrameHeight; y += HeightDivisor)
            {
                if (y > FrameHeight)
                    break;

                for (int x = 0; x < FrameWidth; x += SIZE_DIVISOR)
                {
                    if (x > FrameWidth)
                        break;

                    Vec3b pixelValue = frame.At<Vec3b>(y, x);
                    sb.Append(PixelToAscii(pixelValue));
                }

                sb.Append(Environment.NewLine);
            }

            string asciiFrame = sb.ToString();
            sb.Clear();

            return asciiFrame;
        }

        private static char PixelToAscii(Vec3b pixelIntensity)
        {
            const string asciiCharsSet = " .,:;i1tfLCOG08@#";
            const int maxBrightness = 256 * 3;
            int brightness = pixelIntensity.Item0 + pixelIntensity.Item1 + pixelIntensity.Item2;
            return asciiCharsSet[(brightness * asciiCharsSet.Length) / maxBrightness];
        }

        private static void ClearConsole()
        {
            Console.SetCursorPosition(0, 0);
        }

        private static void WriteToConsole(string asciiFrame)
        {
            ConsoleStream.Write(Encoding.ASCII.GetBytes(asciiFrame), 0, asciiFrame.Length);
        }

        private static void AdjustSleepTime(int frameDelay, Stopwatch sw)
        {
            int elapsedMilliseconds = (int)sw.ElapsedMilliseconds;
            int remainingTime = frameDelay - elapsedMilliseconds;

            if (remainingTime > 0)
                Task.Delay(remainingTime).Wait();

            sw.Restart();
        }

        public static void WriteAudio(int seconds)
        {
            byte[] audioBuffer = new byte[reader.WaveFormat.AverageBytesPerSecond * seconds];
            int bytesRead = reader.Read(audioBuffer, 0, audioBuffer.Length);

            if (bytesRead > 0)
                bufferedWaveProvider.AddSamples(audioBuffer, 0, bytesRead);
        }
    }
}