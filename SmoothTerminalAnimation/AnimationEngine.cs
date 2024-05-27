using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace SmoothTerminalAnimation
{
    interface IAnimationEngine
    {
        void initializeAnimation(int width, int height);
        void showFrames();
    }

    class ScrollerEngine : IAnimationEngine
    {
        private List<AnimationFrame> frames;
        private List<int> frameIndex;
        private IFrameGenerator frameGenerator;
        private int maxFrameCount;
        private int frameCount;
        private int frameDelay;

        public ScrollerEngine(IFrameGenerator frameGenerator, int maxFrameCount, int frameDelay)
        {
            frames = new List<AnimationFrame>();
            frameIndex = new List<int>();
            this.frameGenerator = frameGenerator;
            this.maxFrameCount = maxFrameCount;
            frameCount = 0;
            this.frameDelay = frameDelay;
        }

        private void getFrames()
        {
            while (frameCount < maxFrameCount && frameGenerator.hasNextFrame())
            {
                AnimationFrame frame = frameGenerator.getNextFrame();
                frames.Add(frame);
                frameCount++;
            }
        }

        private static void writeFrameToConsole(AnimationFrame frame)
        {
            Console.ForegroundColor = ConsoleColor.Blue;

            for (int y = 0; y < frame.getHeight(); y++)
            {
                ConsoleColor lastColor = ConsoleColor.White;
                string counter = "";

                for (int x = 0; x < frame.getWidth(); x++)
                {
                    if (frame.getValueAt(x, y) == lastColor)
                        counter += " ";
                    else
                    {
                        Console.BackgroundColor = lastColor;
                        Console.Write(counter);
                        lastColor = frame.getValueAt(x, y);
                        counter = " ";
                    }
                }

                Console.BackgroundColor = lastColor;
                Console.WriteLine(counter);
            }
        }

        private void writeAllFrames()
        {
            foreach (AnimationFrame frame in frames)
            {
                int framePos = Console.GetCursorPosition().Top;
                frameIndex.Add(framePos);
                writeFrameToConsole(frame);
            }
        }

        private void formatConsole(int width, int height)
        {
            int lineCount = maxFrameCount * height + 2;

            Console.SetBufferSize(Console.BufferWidth, lineCount);
            Console.SetWindowSize(width + 1, height);
            Console.Clear();
            Console.SetCursorPosition(0, 0);
        }

        public void initializeAnimation(int width, int height)
        {
            Console.Title = "Formatting Console...";
            formatConsole(width, height);

            Console.Title = "Generating Animation Frames...";
            getFrames();

            Console.Title = "Writing Animation Frames...";
            writeAllFrames();
        }

        public void showFrames()
        {
            int index = 0;
            foreach (int cursorTop in frameIndex)
            {
                Console.Title = "Playing Animation...  (Frame No. " + index + ")";
                Console.SetCursorPosition(0, cursorTop);
                Thread.Sleep(frameDelay);
                index++;
            }
        }
    }

    class DifferentialEngine : IAnimationEngine
    {
        private List<AnimationFrame> frames;
        private AnimationFrame? lastFrame;
        private IFrameGenerator frameGenerator;
        private int maxFrameCount;
        private TimeSpan frameDelay;
        private bool useFrameCache;
        private string?[] stringCache;

        private const long maxCacheSize = 8589934592L; // 1GiB Cache Memory Size Limit

        public DifferentialEngine(IFrameGenerator frameGenerator, int maxFrameCount, double frameDelay)
        {
            frames = new List<AnimationFrame>();
            lastFrame = null;
            this.frameGenerator = frameGenerator;
            this.maxFrameCount = maxFrameCount;
            this.frameDelay = TimeSpan.FromMilliseconds(frameDelay);
            stringCache = new string?[0];
        }

        // Function concept from Guffa at https://codereview.stackexchange.com/a/90281
        private static string makeString(char paddingCharacter, int lenght)
        {
            string resString = paddingCharacter.ToString();

            if (lenght <= resString.Length)
                return resString.Substring(0, lenght);

            while (resString.Length * 2 < lenght)
                resString += paddingCharacter;

            if (resString.Length < lenght)
                resString += resString.Substring(0, lenght - resString.Length);

            return resString;
        }

        private string getStringCached(int length)
        {
            if (stringCache[length] == null)
                stringCache[length] = makeString(' ', length);
            return stringCache[length];
        }

        private void writeFrameToConsole(AnimationFrame currentFrame, AnimationFrame? lastFrame)
        {
            Console.ForegroundColor = ConsoleColor.Blue;

            for (int y = 0; y < currentFrame.getHeight(); y++)
            {
                ConsoleColor lastPrintedColor = ConsoleColor.White;
                int counter = 0;
                bool printing = false;

                for (int x = 0; x < currentFrame.getWidth(); x++)
                {
                    ConsoleColor currentColor = currentFrame.getValueAt(x, y);
                    bool lastFrameColorEqual = lastFrame != null && currentColor == lastFrame.getValueAt(x, y);
                    bool lastPrintedColorEqual = currentColor == lastPrintedColor;

                    if (!printing)
                    {
                        if (lastFrameColorEqual)
                        {
                            continue;
                        }
                        else
                        {
                            Console.SetCursorPosition(x, y);
                            printing = true;

                            counter = 1;
                            lastPrintedColor = currentColor;
                            continue;
                        }
                    }

                    if (!lastPrintedColorEqual)
                    {
                        Console.BackgroundColor = lastPrintedColor;
                        Console.Write(getStringCached(counter));

                        if (!lastFrameColorEqual)
                        {
                            counter = 1;
                            lastPrintedColor = currentColor;
                        }
                        else
                        {
                            printing = false;
                        }
                    }
                    else
                    {
                        counter += 1;
                    }
                }

                if (printing)
                {
                    Console.BackgroundColor = lastPrintedColor;
                    Console.Write(getStringCached(counter));
                }
            }
        }

        private void formatConsole(int width, int height)
        {
            //int lineCount = maxFrameCount * height + 2;

            //Console.SetBufferSize(Console.BufferWidth, lineCount);
            Console.SetWindowSize(width, height);
            //Console.SetBufferSize(Console.BufferWidth, Console.BufferHeight); // To make it not crash on resize - Did not work
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            // Setup String Cache
            stringCache = new string?[width + 1];
            for (int i = 0; i < stringCache.Length; i++)
                stringCache[i] = null;
        }

        private void tryCacheFrames()
        {
            useFrameCache = true;
            int frameCount = 0;
            while (frameCount < maxFrameCount && frameGenerator.hasNextFrame())
            {
                AnimationFrame frame = frameGenerator.getNextFrame();
                frames.Add(frame);
                frameCount++;

                ulong currentCacheMemory = (ulong)frameCount * (ulong)frame.getHeight() * (ulong)frame.getWidth() * 4UL; // ConsoleColor stored in half a byte
                double fullPercentage = Math.Round(((double)currentCacheMemory / (double)maxCacheSize) * 100.0, 2);

                Console.WriteLine("Processed Frame No. " + frameCount + " (~" + currentCacheMemory + "b, " + fullPercentage + "% of Cache)");

                if (currentCacheMemory >= maxCacheSize)
                {
                    frames.Clear();
                    useFrameCache = false;
                    frameGenerator.resetState();
                    Console.WriteLine();
                    Console.WriteLine("Animation too large for maximum cache size of " + maxCacheSize);
                    Console.WriteLine("Aborting frame caching");
                    return;
                }
            }

            Console.Clear();
        }

        private void getFrames(int width, int height)
        {
            string identifier = frameGenerator.getAnimationIdentifier();
            AnimationContainer? container = AnimationContainer.readFromFile(width, height, identifier);

            if (container == null)
                tryCacheFrames();

            if (useFrameCache)
            {
                AnimationFrame[] animationFrames = frames.ToArray();
                container = new AnimationContainer(identifier, ref animationFrames);
                container.saveToFile();
                return;
            }

            if (container != null)
            {
                frames.AddRange(container.GetFrames());
                useFrameCache = true;
            }
        }

        public void initializeAnimation(int width, int height)
        {
            Console.Title = "Formatting Console...";
            formatConsole(width, height);

            Console.Title = "Generating Animation Frames...";
            getFrames(width, height);
        }

        public void showFrames()
        {
            Console.Title = "Playing Animation...";

            if (useFrameCache)
                maxFrameCount = frames.Count();

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < maxFrameCount && (useFrameCache || frameGenerator.hasNextFrame()); i++)
            {
                AnimationFrame frame;
                TimeSpan startGeneration = sw.Elapsed;
                if (useFrameCache)
                    frame = frames[i];
                else
                    frame = frameGenerator.getNextFrame();
                TimeSpan generationDuration = sw.Elapsed - startGeneration;

                TimeSpan startDraw = sw.Elapsed;
                writeFrameToConsole(frame, lastFrame);
                lastFrame = frame;
                TimeSpan drawDuration = sw.Elapsed - startDraw;

                TimeSpan totalDuration = generationDuration + drawDuration;
                TimeSpan delayAfter = frameDelay - totalDuration;
                Console.Title = "Playing Animation...  (Frame No. " + i 
                    + ", Frame Time " + Math.Round(totalDuration.TotalMilliseconds, 2) 
                    + "ms / " + Math.Round(frameDelay.TotalMilliseconds, 2) 
                    + "ms, Draw Time: " + Math.Round(drawDuration.TotalMilliseconds, 2) 
                    + "ms, Generation Time: " + Math.Round(generationDuration.TotalMilliseconds, 2) + "ms)";

                if (delayAfter > TimeSpan.Zero)
                {
                    // Thread.Sleep is very inaccurate
                    //Thread.Sleep(delayAfter);

                    // So spinlock hybrid instead
                    TimeSpan startTime = sw.Elapsed;

                    //int sleepTime = (int)(delayAfter.TotalMilliseconds / 3.5);
                    //if (sleepTime > 3)
                    //    Thread.Sleep(sleepTime);

                    while (sw.Elapsed - startTime < delayAfter)
                    {
                    }
                }
                else
                {
                    int skipFrames = (int)Math.Floor(totalDuration / frameDelay) - 1;
                    Console.Title += " Skipping " + skipFrames + " Frames";

                    frameGenerator.advanceFrames(skipFrames);
                    i += skipFrames;
                }
            }

            if (!useFrameCache)
                frameGenerator.resetState();
        }
    }
}
