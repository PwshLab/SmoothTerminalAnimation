using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothTerminalAnimation
{
    interface IProcessOrchestrator
    {
        void initGenerator();
        void initEngine();
        void playAnimation();
    }

    class AnimationOrchestrator : IProcessOrchestrator
    {
        private IAnimationEngine? engine;
        private IFrameGenerator? generator;
        private int width, height, fps, duration;

        public AnimationOrchestrator(int width, int height, int fps, int duration)
        {
            this.width = width;
            this.height = height;
            this.fps = fps;
            this.duration = duration;
        }

        public void initGenerator()
        {
            Console.Title = "Configuring Frame Generator...";

            int surface = 2 * width + 2 * height - 4;
            int volume = width * height / 8;
            bool[,] field = new bool[width, height];

            Random rand = new Random();
            for (int i = 0; i < volume; i++)
            {
                int x = rand.Next(width);
                int y = rand.Next(height);
                field[x, y] = true;
            }

            generator = new GameOfLife(field);
        }

        public void initEngine()
        {
            Console.Title = "Configuring Animation Engine...";

            if (generator == null)
                return;

            int frameTime = 1000 / fps;
            int frameCount = duration * fps;

            //engine = new ScrollerEngine(generator, frameCount, frameTime);
            engine = new DifferentialEngine(generator, frameCount, frameTime);

            engine.initializeAnimation(width, height);
        }

        public void playAnimation()
        {
            Console.Title = "Playing Animation...";

            if (engine == null)
                return;

            while (true)
            {
                engine.showFrames();
            }
        }
    }

    class VideoOrchestrator : IProcessOrchestrator
    {
        private IAnimationEngine? engine;
        private IFrameGenerator? generator;
        private int width, height, fps;
        private string videoPath;
        private bool isFilePath;

        public VideoOrchestrator(int width, int height, int fps, string videoPath, bool isFilePath)
        {
            this.width = width;
            this.height = height;
            this.fps = fps;
            this.videoPath = videoPath;
            this.isFilePath = isFilePath;
        }

        public void initGenerator()
        {
            Console.Title = "Configuring Frame Generator...";

            if (isFilePath)
                generator = new VideoFrames(width, height, fps, videoPath);
            else
                generator = new VideoFrames(width, height, fps, new Uri(videoPath));
        }

        public void initEngine()
        {
            Console.Title = "Configuring Animation Engine...";

            if (generator == null)
                return;

            double frameTime = 1000.0 / (double)fps;
            //int frameCount = duration * fps;

            engine = new DifferentialEngine(generator, int.MaxValue, frameTime);

            engine.initializeAnimation(width, height);
        }

        public void playAnimation()
        {
            Console.Title = "Playing Animation...";

            if (engine == null)
                return;

            while (true)
            {
                engine.showFrames();
            }
        }
    }
}
