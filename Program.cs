using System;
using System.Diagnostics;
using System.Text;
using OpenCvSharp;

namespace VideoToAsciiConverter
{
    class Program
    {
        private static readonly Stream consoleStream = Console.OpenStandardOutput();
        private static VideoCapture capture;
        private const int fpsDivisor = 2;
        private const int sizeDivisor = 2;

        static void Main()
        {
            string videoPath = @"C:\Users\gusta\Documents\GitHub\video-to-ascii-converter\videoplayback.mp4";
            capture = new VideoCapture(videoPath, VideoCaptureAPIs.ANY);

            if (!capture.IsOpened())
            {
                Console.WriteLine("Error opening the video!");
                return;
            }

            // Console Setup
            Console.Title = Path.GetFileNameWithoutExtension(videoPath);
            Console.OutputEncoding = Encoding.ASCII;

            // Set process priority
            using var currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityBoostEnabled = true;
            currentProcess.PriorityClass = ProcessPriorityClass.RealTime;

            // Set console window size
            Console.SetWindowSize(((int)capture.FrameWidth / sizeDivisor) + 1, ((int)capture.FrameHeight / sizeDivisor * 2) + 1);

            ProcessVideo();
        }

        private static void ProcessVideo()
        {
            int frameWidth = (int)capture.FrameWidth;
            int frameHeight = (int)capture.FrameHeight;
            int heightDivisor = sizeDivisor * 2;

            var stopwatch = new Stopwatch();
            var timeIntegrity = new Stopwatch();
            var stringBuilder = new StringBuilder();
            var rowIndexes = new int[(frameHeight / heightDivisor) + 4];

            int currentFrame = (int)capture.Fps / fpsDivisor;
            int frameDelay = (int)(1000 / ((int)capture.Fps / fpsDivisor));
            int currentSecond = 0;

            timeIntegrity.Restart();

            Mat frame = new Mat();

            while (true)
            {
                capture.Read(frame);

                if (frame.Empty())
                    break;

                for (int i = 0; i < fpsDivisor - 1; i++)
                    capture.Grab();

                if (currentFrame >= (int)capture.Fps / fpsDivisor)
                {
                    AdjustFrameDelay(ref frameDelay, ref currentSecond, timeIntegrity);
                    currentFrame = 0;
                    currentSecond++;
                }

                string asciiFrame = ConvertFrameToAscii(frame, frameWidth, frameHeight, heightDivisor, stringBuilder, rowIndexes);
                WriteToConsole(Encoding.UTF8.GetBytes(asciiFrame));

                currentFrame++;
                AdjustSleepTime(frameDelay, stopwatch);

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    break;
            }
        }

        private static void AdjustFrameDelay(ref int frameDelay, ref int currentSecond, Stopwatch timeIntegrity)
        {
            long difference = 0;

            if (currentSecond > 0)
                difference = (timeIntegrity.ElapsedMilliseconds / currentSecond) - 1000;

            if (timeIntegrity.ElapsedMilliseconds / 1000 < currentSecond)
            {
                int timeToWait = (currentSecond * 1000) - (int)timeIntegrity.ElapsedMilliseconds - 1;

                if (timeToWait > frameDelay)
                    frameDelay++;

                Thread.Sleep(timeToWait);
            }
            else if (difference > frameDelay && frameDelay > 0)
            {
                frameDelay--;
            }
        }

        private static string ConvertFrameToAscii(Mat frame, int frameWidth, int frameHeight, int heightDivisor, StringBuilder sb, int[] rowIndexes)
        {
            for (int y = 0; y < frameHeight; y += heightDivisor)
            {
                for (int x = 0; x < frameWidth; x += sizeDivisor)
                {
                    Vec3b pixelValue = frame.At<Vec3b>(y, x);
                    sb.Append(PixelToAscii(pixelValue));
                }

                sb.Append(Environment.NewLine);
            }

            int currentRowIndex = 0;

            string frameString = string.Join("\n", sb.ToString().Split('\n').Select(line =>
            {
                string trimmed = line.TrimEnd();
                int trimmedLength = trimmed.Length;
                int diff = rowIndexes[currentRowIndex] - trimmedLength;

                rowIndexes[currentRowIndex++] = trimmedLength;
                return diff <= 0 ? trimmed : trimmed + new string(' ', diff);
            }));

            sb.Clear();
            return frameString;
        }

        private static char PixelToAscii(Vec3b pixelIntensity)
        {
            const string asciiCharsSet = " .,:;i1tfLCOG08@#";
            const int maxBrightness = 256 * 3;
            int brightness = pixelIntensity.Item0 + pixelIntensity.Item1 + pixelIntensity.Item2;
            return asciiCharsSet[(brightness * asciiCharsSet.Length) / maxBrightness];
        }

        private static void WriteToConsole(byte[] buffer)
        {
            consoleStream.Write(buffer, 0, buffer.Length);
        }

        private static void AdjustSleepTime(int frameDelay, Stopwatch sw)
        {
            if (sw.ElapsedMilliseconds < frameDelay)
                Thread.Sleep(frameDelay - (int)sw.ElapsedMilliseconds);

            sw.Restart();
        }
    }
}