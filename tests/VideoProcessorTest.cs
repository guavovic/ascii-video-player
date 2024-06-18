//using Emgu.CV;

//namespace VideoToAsciiConverter.tests
//{
//    internal class VideoProcessorTest
//    {
//        static void Main(string[] args)
//        {
//            const string videoPath = @"C:\Users\gusta\Documents\GitHub\video-to-ascii-converter\teste.mp4";
//            VideoCapture cap = new VideoCapture(videoPath);

//            if (!cap.IsOpened)
//            {
//                Console.WriteLine("Erro ao abrir o vídeo!");
//                return;
//            }

//            Mat frame = new Mat();

//            while (true)
//            {
//                cap.Read(frame);

//                if (frame.IsEmpty)
//                    break;

//                CvInvoke.Imshow("Frame", frame);

//                if (CvInvoke.WaitKey(30) == 27) // Pressione ESC para sair
//                    break;
//            }

//            cap.Dispose();
//            CvInvoke.DestroyAllWindows();
//        }
//    }
//}
