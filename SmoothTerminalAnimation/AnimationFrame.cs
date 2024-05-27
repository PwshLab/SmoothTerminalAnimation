using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothTerminalAnimation
{
    class AnimationFrame
    {
        //private ConsoleColor[,] colorField;
        private int frameHeigth, frameWidth;
        private byte[] colorField;

        public AnimationFrame(int width, int height)
        {
            frameWidth = width;
            frameHeigth = height;
            //colorField = new ConsoleColor[width, height];
            int fieldSize = (width * height) / 2 + 1;
            colorField = new byte[fieldSize];

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Width or Height cannot be zero or negative");
            }
        }

        public AnimationFrame(int width, int height, byte[] colorField)
        {
            frameWidth = width;
            frameHeigth = height;
            this.colorField = colorField;
        }

        private static byte consoleColorToByte(ConsoleColor input1, ConsoleColor input2)
        {
            byte first = (byte)((byte)input1 << 4);
            byte second = (byte)input2;

            byte data = (byte)(first | second);
            return data;
        }

        private static ConsoleColor byteToConsoleColor(byte input, bool first)
        {
            const byte bitmaskFirst = 0b_1111_0000;
            const byte bitmaskSecond = 0b_0000_1111;

            int data;
            if (first)
                data = (bitmaskFirst & input) >> 4;
            else
                data = bitmaskSecond & input;

            return (ConsoleColor)data;
        }

        public void fromColorArray(ref ConsoleColor[] colorArray)
        {
            if (colorArray.Length != frameWidth * frameHeigth)
            {
                throw new ArgumentException("Array length does not equal frame size ( " + frameHeigth + "*" + frameWidth + " )");
            }

            //int index = 0;
            //for (int y = 0; y < frameHeigth; y++)
            //{
            //    for (int x = 0; x < frameWidth; x++)
            //    {
            //        colorField[x, y] = colorArray[index];
            //        index++;
            //    }
            //}

            int index = 0;
            int offset = colorArray.Length % 2;
            for (int i = 0; i < colorArray.Length - offset; i += 2)
            {
                byte data = consoleColorToByte(colorArray[i], colorArray[i + 1]);
                colorField[index++] = data;
            }

            if (offset == 1)
                colorField[index++] = consoleColorToByte(colorArray[colorArray.Length - 1], ConsoleColor.Black);
        }

        public int getWidth()
        {
            return frameWidth;
        }

        public int getHeight()
        {
            return frameHeigth;
        }

        public ConsoleColor getValueAt(int width, int height)
        {
            if (width >= frameWidth || height >= frameHeigth)
            {
                throw new IndexOutOfRangeException();
            }

            //return colorField[width, height];

            int rawFieldIndex = frameWidth * height + width;
            byte data = colorField[rawFieldIndex / 2];
            bool isFirst = rawFieldIndex % 2 == 0;
            ConsoleColor color = byteToConsoleColor(data, isFirst);
            return color;
        }

        public byte[] getData()
        {
            return colorField;
        }
    }

    class AnimationContainer
    {
        private string animationIdentifier;
        private int frameWidth, frameHeight;
        private AnimationFrame[] frames;

        public AnimationContainer(string animationIdentifier, ref AnimationFrame[] frames) 
        { 
            this.animationIdentifier = animationIdentifier;
            this.frames = frames;

            if (frames.Length > 0)
            {
                frameWidth = frames[0].getWidth();
                frameHeight = frames[0].getHeight();
            }
            else
                throw new ArgumentException();
        }

        public AnimationFrame[] GetFrames()
        {
            return frames;
        }

        private static string getFilePath(int width, int height, string identifier)
        {
            string resolution = "" + width + "x" + height;
            string fileName = resolution + "-" + identifier + ".afc";
            string filePath = Path.Combine(Path.GetTempPath(), fileName);
            return filePath;
        }

        public void saveToFile()
        {
            Console.WriteLine("Freezing animation container data...");

            string filePath = getFilePath(frameWidth, frameHeight, animationIdentifier);
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    byte[] data = frames[i].getData();
                    stream.Write(data, 0, data.Length);
                }
            }

            Console.WriteLine("Saved data to " + filePath);
        }

        public static bool containerExists(int width, int height, string identifier)
        {
            string filePath = getFilePath(width, height, identifier);
            return File.Exists(filePath);
        }

        public static AnimationContainer? readFromFile(int width, int height, string identifier)
        {
            Console.WriteLine("Reading animation container data...");

            string filePath = getFilePath(width, height, identifier);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Frame container " + filePath + " does not exist");
                return null;
            }

            AnimationFrame exampleFrame = new AnimationFrame(width, height);
            int frameSize = exampleFrame.getData().Length;

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            int frameCount = (int)(stream.Length / frameSize);
            AnimationFrame[] frames = new AnimationFrame[frameCount];

            for (int i = 0; i < frames.Length; i++)
            {
                byte[] buffer = new byte[frameSize];
                stream.Read(buffer, 0, frameSize);
                frames[i] = new AnimationFrame(width, height, buffer);
            }

            AnimationContainer container = new AnimationContainer(identifier, ref frames);

            Console.WriteLine("Read animation container data from " + filePath);

            return container;
        }
    }
}
