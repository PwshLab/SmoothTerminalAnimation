using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;

namespace SmoothTerminalAnimation
{
    interface IFrameGenerator
    {
        AnimationFrame getNextFrame();
        bool hasNextFrame();

        void resetState();

        void advanceFrames(int count);

        string getAnimationIdentifier();
    }

    class GameOfLife : IFrameGenerator
    {
        private bool[,] initialGameBoard;
        private bool[,] gameBoard;
        private bool hasChange;

        public GameOfLife(int width, int height)
        {
            initialGameBoard = new bool[width, height];
            gameBoard = new bool[width, height];
            hasChange = true;
        }

        public GameOfLife(bool[,] initialGameBoard)
        {
            this.initialGameBoard = initialGameBoard;
            gameBoard = initialGameBoard;
            hasChange = true;
        }

        private bool getValueAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= gameBoard.GetLength(0) || y >= gameBoard.GetLength(1))
            {
                return false;
            }

            return gameBoard[x, y];
        }

        private int getNeighbours(int x, int y)
        {
            int[][] positions = { new int[]{ -1, -1 }, new int[]{  0, -1 }, new int[]{  1, -1 },
                                  new int[]{ -1,  0 },                      new int[]{  1,  0 },
                                  new int[]{ -1,  1 }, new int[]{  0,  1 }, new int[]{  1,  1 }, };
            int count = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                if (getValueAt(x + positions[i][0], y + positions[i][1]))
                {
                    count++;
                }
            }

            return count;
        }

        // Rules from https://en.wikipedia.org/wiki/Conway's_Game_of_Life#Rules
        private void performTick()
        {
            bool[,] nextGameBoard = new bool[gameBoard.GetLength(0), gameBoard.GetLength(1)];

            for (int x = 0; x < gameBoard.GetLength(0); x++)
            {
                for (int y = 0; y < gameBoard.GetLength(1); y++)
                {
                    int adj = getNeighbours(x, y);
                    if (gameBoard[x, y])
                    {
                        if (adj < 2 || adj > 3)
                            nextGameBoard[x, y] = false;
                        else
                            nextGameBoard[x, y] = true;
                    }
                    else
                    {
                        if (adj == 3)
                            nextGameBoard[x, y] = true;
                    }
                }
            }

            if (nextGameBoard.Equals(gameBoard))
                hasChange = false;

            gameBoard = nextGameBoard;
        }

        private AnimationFrame toFrame()
        {
            ConsoleColor[] colors = new ConsoleColor[gameBoard.GetLength(0) * gameBoard.GetLength(1)];
            int index = 0;
            for (int y = 0; y < gameBoard.GetLength(1); y++)
            {
                for (int x = 0; x < gameBoard.GetLength(0); x++)
                {
                    if (gameBoard[x, y])
                        colors[index] = ConsoleColor.DarkGray;
                    else
                        colors[index] = ConsoleColor.Gray;
                    index++;
                }
            }

            AnimationFrame frame = new AnimationFrame(gameBoard.GetLength(0), gameBoard.GetLength(1));
            frame.fromColorArray(ref colors);
            return frame;
        }

        public AnimationFrame getNextFrame()
        {
            AnimationFrame frame = toFrame();
            performTick();
            return frame;
        }

        public bool hasNextFrame()
        {
            return hasChange;
        }

        public void resetState()
        {
            gameBoard = initialGameBoard;
            hasChange = true;
        }

        public void advanceFrames(int count)
        {
            for (int i = 0; i < count; i++)
                performTick();
        }

        public string getAnimationIdentifier()
        {
            byte[] data = new byte[initialGameBoard.Length];
            int index = 0;
            foreach (bool point in initialGameBoard)
            {
                data[index++] = (byte)(point ? 1 : 0);
            }
            return FileHash.GetDataHash(data);
        }
    }

    class VideoFrames : IFrameGenerator
    {
        private string videoFilePath, videoFrameFolderPath;
        private int width, height, fps, currentFrame;
        private string[] framePaths;
        private bool isInitialized, deferedInitialization;

        public VideoFrames(int width, int height, int fps, Uri videoUri)
        {
            this.width = width;
            this.height = height;
            this.fps = fps;
            currentFrame = 0;

            videoFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
            downloadVideo(videoUri);

            videoFrameFolderPath = "";
            framePaths = new string[0];

            initializeGenerator();
        }

        public VideoFrames(int width, int height, int fps, string videoPath)
        {
            this.width = width;
            this.height = height;
            this.fps = fps;
            currentFrame = 0;
            videoFilePath = videoPath;

            videoFrameFolderPath = "";
            framePaths = new string[0];

            initializeGenerator();
        }

        private void initializeGenerator()
        {
            if (isInitialized)
                return;

            if (!deferedInitialization)
                if (checkCache())
                {
                    deferedInitialization = true;
                    return;
                }

            Console.WriteLine("Initialized frame Generator");

            videoFrameFolderPath = getNewTempFolderPath(FileHash.GetFileHash(videoFilePath) + Path.GetRandomFileName());
            setupFFMpeg();
            convertVideo();
            framePaths = getFramePaths();
            isInitialized = true;
        }

        private static string getNewTempFolderPath(string folderName)
        {
            string tempFolderPath = Path.GetTempPath();
            string childPath = Path.Combine(tempFolderPath, folderName);
            if (!Directory.Exists(childPath))
                Directory.CreateDirectory(childPath);
            return Path.GetFullPath(childPath);
        }

        private void downloadVideo(Uri videoUri)
        {
            Console.Title = "Downloading Target Video...";

            using FileStream stream = new FileStream(videoFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            FileDownload.Download(videoUri, stream);
            stream.Flush();
            stream.Close();
        }

        private void convertVideo()
        {
            Console.Title = "Converting Target Video...";

            FFMpegArguments
                .FromFileInput(videoFilePath)
                .OutputToFile("%08d.png", true, options => options
                    .WithFramerate((double)fps)
                    .WithVideoCodec(VideoCodec.Png)
                    .OverwriteExisting()
                    .Resize(width, height))
                .Configure(options => options.WorkingDirectory = videoFrameFolderPath)
                .ProcessSynchronously();
        }

        private string[] getFramePaths()
        {
            // A little bit of Linq, as a treat
            // Should only be executed once, so its impact should be low
            return Directory.GetFiles(videoFrameFolderPath).Where(s => Path.GetExtension(s) == ".png").ToArray();
        }

        private void setupFFMpeg()
        {
            Console.Title = "Configuring FFMpeg...";

            const string ffmpegUrl = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip";
            string ffmpegDir = getNewTempFolderPath("ffmpeg");

            if (!File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")))
            {
                Console.Title = "Downloading FFMpeg...";

                using MemoryStream stream = new MemoryStream();
                FileDownload.Download(new Uri(ffmpegUrl), stream);

                Console.Title = "Extracting FFMpeg...";

                ZipArchive archive = new ZipArchive(stream);
                archive.ExtractToDirectory(ffmpegDir, true);
            }

            GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegDir);
        }

        private AnimationFrame convertFileToFrame(string filePath)
        {
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Bitmap bitmap = new Bitmap(stream);
            byte[] bitmapData = BitmapReader.GetBitmapAsArrayBGR(bitmap);
            ConsoleColor[] colors = new ConsoleColor[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                int dataIndex = i * 3;
                if (dataIndex + 2 < bitmapData.Length)
                    colors[i] = ColorMapper.ClosestConsoleColor(bitmapData[dataIndex + 2], bitmapData[dataIndex + 1], bitmapData[dataIndex]);
                else
                    colors[i] = ConsoleColor.Black;
            }

            AnimationFrame frame = new AnimationFrame(width, height);
            frame.fromColorArray(ref colors);
            return frame;
        }

        public AnimationFrame getNextFrame()
        {
            initializeGenerator();
            string currentPath = framePaths[currentFrame++];
            AnimationFrame frame = convertFileToFrame(currentPath);
            return frame;
        }

        public bool hasNextFrame()
        {
            return currentFrame < framePaths.Length;
        }

        public void resetState()
        {
            currentFrame = 0;
        }

        public void advanceFrames(int count)
        {
            currentFrame += count;
        }

        public string getAnimationIdentifier()
        {
            return FileHash.GetFileHash(videoFilePath);
        }

        private bool checkCache()
        {
            return AnimationContainer.containerExists(width, height, getAnimationIdentifier());
        }
    }
}
