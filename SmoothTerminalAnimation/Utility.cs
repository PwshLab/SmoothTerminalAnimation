using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SmoothTerminalAnimation
{
    static class ColorMapper
    {
        // Code with slight modifications by Glenn Slayden on Stackoverflow https://stackoverflow.com/a/12340136
        public static ConsoleColor ClosestConsoleColorLegacy(byte r, byte g, byte b)
        {
            ConsoleColor ret = 0;
            double rr = r, gg = g, bb = b, delta = double.MaxValue;

            foreach (ConsoleColor cc in Enum.GetValues(typeof(ConsoleColor)))
            {
                var n = Enum.GetName(typeof(ConsoleColor), cc);
                var c = System.Drawing.Color.FromName(n == "DarkYellow" ? "Orange" : n); // bug fix
                var t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0);
                if (t == 0.0)
                    return cc;
                if (t < delta)
                {
                    delta = t;
                    ret = cc;
                }
            }
            return ret;
        }

        
        private static readonly Color[] consoleColorValues = GetConsoleColorValues();

        private static Color[] GetConsoleColorValues()
        {
            List<Color> colors = new List<Color>();
            for (int i = 0; i < 16; i++)
            {
                ConsoleColor consoleColor = (ConsoleColor)i; // Replace Enum Enumeration like Enum.GetValues(typeof(ConsoleColor))

                string? colorName = Enum.GetName(typeof(ConsoleColor), consoleColor);
                if (colorName == null)
                    continue;

                if (colorName == "DarkYellow")
                    colorName = "Orange";

                Color baseColor = Color.FromName(colorName);
                colors.Add(baseColor);
            }

            //if (colors.Count != 16)
            //    throw new Exception("Console Color Fuckery! This should be impossible"); // Sanity Check

            return colors.ToArray();
        }

        // Based on ClosestConsoleColor Function
        public static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b)
        {
            int closestColorIndex = 0;
            double smallestDistance = double.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                Color consoleColorValue = consoleColorValues[i];

                double rPart = consoleColorValue.R - r, gPart = consoleColorValue.G - g, bPart = consoleColorValue.B - b;
                double currentDistance = (rPart * rPart) + (gPart * gPart) + (bPart * bPart);

                if (currentDistance == 0.0)
                    return (ConsoleColor)i;

                if (currentDistance < smallestDistance)
                {
                    smallestDistance = currentDistance;
                    closestColorIndex = i;
                }
            }

            return (ConsoleColor)closestColorIndex;
        }
    }

    static class FileDownload
    {
        public static void Download(Uri uri, Stream outputStream)
        {
            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = httpClient.Send(request);
            response.EnsureSuccessStatusCode();
            HttpContent content = response.Content;

            content.CopyTo(outputStream, null, new CancellationToken());
        }
    }

    static class FileHash
    {
        public static string GetFileHash(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(stream);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return sb.ToString();
        }

        public static string GetDataHash(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            using (MemoryStream stream = new MemoryStream(data))
            {
                SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(stream);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }

    static class BitmapReader
    {
        public static byte[] GetBitmapAsArrayBGR(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
                );
            IntPtr dataStart = bitmapData.Scan0;
            byte[] imageData = new byte[bitmapData.Width * bitmapData.Height * 3];
            int lineDataLength = bitmapData.Width * 3;

            for (int i = 0; i < bitmapData.Height; i++)
            {
                int offset = lineDataLength * i;
                IntPtr copyDataStart = dataStart + bitmapData.Stride * i;
                Marshal.Copy(copyDataStart, imageData, offset, lineDataLength);
            }
            
            return imageData;
        }
    }
}
