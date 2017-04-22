using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using System.Windows;

namespace RawLibrary
{
    [Flags]
    public enum PixelType { Blue = 1, GreenEven = 2, Red = 4, GreenOdd = 8 }

    public class RawImage
    {
        private readonly BayerAlgorithm _algorithm;
        public readonly RawV3 Raw;

        public int SensorWidth { get; } // sensor width
        public int SensorHeight { get; } // sensor height
        public int ImageWidth { get; } // image width
        public int ImageHeight { get; } // image height
        public long FrameSizeInPixels { get; private set; } // Width * Height
        public SensorType SensorType { get; }
        public bool IsRaw { get; private set; }
        public bool HasKnownSensorType { get; private set; }
        public ulong NofFrames { get; private set; }

        public WriteableBitmap Source { get; }
        public BitmapSource BlankFrame { get; private set; }

        public RawImage(FileSystemInfo file) : this(file, SensorType.BG_GR) { }

        public RawImage(FileSystemInfo file, SensorType sensorType)
        {
            SensorType = sensorType;
            if (file.Extension.Equals(".raw3"))
            {
                Raw = new RawV3(file.FullName);
                SensorWidth = (int)Raw.Width;
                SensorHeight = (int)Raw.Height;
                ImageWidth = (int)Raw.ImageWidth;
                ImageHeight = (int)Raw.ImageHeight;
                FrameSizeInPixels = (long)Raw.Width * Raw.Height;
                SensorType = Raw.SensorType;
                IsRaw = true;
                HasKnownSensorType = true;
                NofFrames = Raw.NofFrames;

                Source = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr32, null); // cs ImageWidth ImageHeight
                BlankFrame = CreateBlankFrame();
            }
            if (SensorType != SensorType.Mono)
                _algorithm = new BayerSimple(this);
        }

        public void ReadFirstFrame()
        {
            ReadFrame(0);
        }

        public void ReadFrame(ulong framenumber)
        {
            if (Raw != null)
            {
                Raw.ReadFrame(framenumber);
                UpdateSource();
            }
        }

        public void ReadNextFrame()
        {
            if (Raw != null)
            {
                Raw.ReadFrame();
                UpdateSource();
            }
        }

        public void ReadPreviousFrame()
        {
            if (Raw != null)
            {
                Raw.ReadPreviousFrame();
                UpdateSource();
            }
        }

        /// <summary>
        /// Operates directly on the WriteableBitmap handed over as parameter
        /// to prevent creations of unneccessary additional objects and references.
        /// </summary>
        private void UpdateSource()
        {
            // Reserve the back buffer for updates.
            Source.Lock();

            if (SensorType == SensorType.Mono) {
                PixelFormat pixelFormat = Source.Format;
                int bypp = pixelFormat.BitsPerPixel / 8;
                int stride = ImageWidth * bypp;

                unsafe
                {
                    // Get a pointer to the back buffer.
                    IntPtr pBackBuffer = Source.BackBuffer;
                    int srcPos = 0;
                    int srcPadding = SensorWidth - ImageWidth;
                    int dstPadding = Source.BackBufferStride - stride;

                    for (int y=0; y < ImageHeight; y++) {
                        for(int x=0; x < ImageWidth; x++, srcPos++, pBackBuffer += bypp) {
                            byte val = Raw.Data[srcPos];
                            uint color = (uint)(val << 16 | val << 8 | val);

                            *(uint*)pBackBuffer.ToPointer() = color;
                        }
                        srcPos += srcPadding;
                        pBackBuffer += dstPadding;
                    }
                }
            } else {
                _algorithm.Convert(Source);
            }
            
            // Specify the area of the bitmap that has been changed.
            Source.AddDirtyRect(new Int32Rect(0, 0, ImageWidth, ImageHeight));

            // Release the back buffer and make it available for display.
            Source.Unlock();
        }

        /// <summary>
        /// Creates a smallest possible BlankFrame with the same aspect ratio as the video frames.
        /// To be displayed when the particular video has finished while others haven't yet.
        /// </summary>
        /// <returns></returns>
        private BitmapSource CreateBlankFrame()
        {
            int gcd = (int)BigInteger.GreatestCommonDivisor(ImageWidth, ImageHeight);
            BitmapPalette palette = new BitmapPalette(new List<Color> { Colors.DarkGray });

            int width = ImageWidth / gcd;
            int height = ImageHeight / gcd;
            PixelFormat format = PixelFormats.Indexed1;
            int stride = width / 8 + 1;
            byte[] data = new byte[height * stride];

            return BitmapSource.Create(width, height, 1, 1, format, palette, data, stride);
        }
    }
}
