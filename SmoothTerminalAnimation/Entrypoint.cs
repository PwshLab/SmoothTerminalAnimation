using System.Diagnostics;

namespace SmoothTerminalAnimation
{
    static class Entrypoint
    {
        public static void RunAnimation(int width, int height, int fps, int duration)
        {
            AnimationOrchestrator po = new AnimationOrchestrator(width, height, fps, duration);
            po.initGenerator();
            po.initEngine();
            po.playAnimation();
        }

        public static void RunVideo(int width, int height, int fps, string videoPath, bool isFilePath)
        {
            VideoOrchestrator vo = new VideoOrchestrator(width, height, fps, videoPath, isFilePath);
            vo.initGenerator();
            vo.initEngine();
            vo.playAnimation();
        }

        public static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            //if (args.Length < 1)
            //    return;

            // Scroller Settings
            //int n = 53;
            //Run(n * 3, n, 10, 10);
            
            string? read;
            if (args.Length == 0)
            {
                Console.WriteLine("Resize console to target size, then press enter");
                read = Console.ReadLine();
            }
            else
            {
                read = args[0];
            }

            // Differential Settings
            //RunAnimation(Console.WindowWidth, Console.WindowHeight, 60, 60);

            const string videoUri1 = "https://github.com/ShatteredDisk/rickroll/raw/master/rickroll.mp4";
            const string videoUri2 = "https://ia801509.us.archive.org/10/items/Rick_Astley_Never_Gonna_Give_You_Up/Rick_Astley_Never_Gonna_Give_You_Up.mp4";

            //string videoPath = args[0];

            bool parsed = false;

            if (read != null && Uri.IsWellFormedUriString(read, UriKind.Absolute))
            {
                parsed = true;
                Console.WriteLine("Input parsed as Uri");
                try
                {
                    RunVideo(Console.WindowWidth, Console.WindowHeight, 60, read, false);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Input isnt a valid uri");

            if (parsed)
                return;

            if (read != null && File.Exists(read))
            {
                parsed = true;
                Console.WriteLine("Input parsed as file path");
                try
                {
                    RunVideo(Console.WindowWidth, Console.WindowHeight, 60, read, true);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (parsed)
                return;

            Console.WriteLine("Input isnt a valid file path");
            Console.WriteLine();
            Console.WriteLine("Falling back on default video source");

            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(1000);
                Console.Write(".");
            }

            Console.WriteLine();

            //RunVideo(Console.WindowWidth, Console.WindowHeight, 60, videoUri, false);

            try
            {
                RunVideo(Console.WindowWidth, Console.WindowHeight, 60, videoUri1, false);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Video link expired");
            Console.WriteLine();
            Console.WriteLine("Falling back on secondary video source");

            try
            {
                RunVideo(Console.WindowWidth, Console.WindowHeight, 60, videoUri2, false);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}