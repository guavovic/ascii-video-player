//using OpenCvSharp;

//namespace VideoToAsciiConverter.tests
//{
//    internal class VideoProcessorTest
//    {
//        static void Main(string[] args)
//        {
//            const string videoPath = @"C:\Users\gusta\Documents\GitHub\video-to-ascii-converter\teste.mp4";
//            VideoCapture cap = new VideoCapture(videoPath, VideoCaptureAPIs.ANY);

//            if (!cap.IsOpened())
//            {
//                Console.WriteLine("Erro ao abrir o vídeo!");
//                return;
//            }

//            Mat frame = new Mat();

//            while (true)
//            {
//                cap.Read(frame);

//                if (frame.Empty())
//                    break;

//                Cv2.ImShow("Frame", frame);

//                if (Cv2.WaitKey(30) == 27) // Wait for 'ESC' key press for 30ms
//                    break;
//            }

//            cap.Release();
//            Cv2.DestroyAllWindows();
//        }
//    }
//}
