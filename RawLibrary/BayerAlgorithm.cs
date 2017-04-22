using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RawLibrary
{
    public abstract class BayerAlgorithm
    {
        #region "Member"
        protected byte[] m_Raw;      // input : bayerpattern pixel
        protected byte[] m_Red;      // output: red channel
        protected byte[] m_Grn;      // output: green channel
        protected byte[] m_Blu;      // output: blue channel

        protected byte[] m_Eq3;      // horizontal green interpolation
        protected byte[] m_Eq4;      // vertical green interpolation
        protected byte[] m_Eq5;      // cross green interpolation
        protected double[] m_Eq6;
        protected double[] m_Eq7;
        protected double[] m_EqE;
        protected int[] m_CdiffH;   // horizontal color differences
        protected int[] m_CdiffV;
        protected int[] m_CdiffBh;
        protected int[] m_CdiffBv;

        public double m_threshold = 2.0;

        protected RawImage RawImage { get; }
        public string Name { get; private set; }

        public int Width => RawImage.SensorWidth;
        public int Height => RawImage.SensorHeight;
        public long FrameSize => RawImage.FrameSizeInPixels;
        public SensorType SensorType => RawImage.SensorType;

        #endregion

        #region "Constructor"
        protected BayerAlgorithm(RawImage rawfile, string name)
        {
            RawImage = rawfile;
            m_Raw = rawfile.Raw.Data;
            Name = name;
        }
        #endregion

        #region
        protected virtual void InterpolateGreen() { }
        public virtual void Convert(WriteableBitmap bitmap) { }

        protected int CheckPixelIndex(int x, int y, int offsetX, int offsetY, int pos)
        {
            if (x + offsetX < 0 || x + offsetX >= Width)
            {
                pos -= offsetX;
            }
            else
            {
                pos += offsetX;
            }

            if (y + offsetY < 0 || y + offsetY >= Height)
            {
                pos -= offsetY * Width;
            }
            else
            {
                pos += offsetY * Width;
            }
            return pos;
        }


        /// <summary>
        /// copy the existing pixels from the bayer pattern to the RGB-outputimage
        /// </summary>      
        protected void CopyRawToRGB()
        {
            m_Raw = RawImage.Raw.Data;
            m_Red = (byte[])m_Raw.Clone();
            m_Grn = (byte[])m_Raw.Clone();
            m_Blu = (byte[])m_Raw.Clone();
        }

        protected void CopyRGBtoBitmap(WriteableBitmap bitmap)
        {
            PixelFormat pixelFormat = bitmap.Format;
            int bypp = pixelFormat.BitsPerPixel/8;
            int w = RawImage.ImageWidth;
            int h = RawImage.ImageHeight;
            int srcPadding = Width - w;
            int dstPadding = bitmap.BackBufferStride - w*bypp;

            unsafe
            {
                // Get a pointer to the back buffer.
                IntPtr pBackBuffer = bitmap.BackBuffer;
                int srcPos = 0;

                // copy the 3 channels to one image
                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++, srcPos++, pBackBuffer += bypp) {
                        uint color = (uint)(m_Red[srcPos] << 16 | m_Grn[srcPos] << 8 | m_Blu[srcPos]);

                        *(uint*)pBackBuffer.ToPointer() = color;
                    }
                    srcPos += srcPadding;
                    pBackBuffer += dstPadding;
                }
            }
        }

        protected void InterpolateGreenBorderPixel()
        {
            // interpolate border pixel
            int x, y, i, i1, i2, i3, i4;

            // top and bottom pixel
            // set startposition of nongreen pixels in the first four lines
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB)
            {
                i1 = 2;
                i2 = Width + 1;
                i3 = 2 * Width + 2;
                i4 = 3 * Width + 1;
            }
            else {
                i1 = 1;
                i2 = Width + 2;
                i3 = 2 * Width + 1;
                i4 = 3 * Width + 2;
            }
            for (x = 1; x < Width - 1; x += 2)
            {
                m_Grn[i1] = PixelMath.ByteClamp((m_Grn[i1 - 1] + m_Grn[i1 + 1]) >> 1);
                m_Grn[i2] = PixelMath.ByteClamp((m_Grn[i2 - 1] + m_Grn[i2 + 1]) >> 1);
                m_Grn[i3] = PixelMath.ByteClamp((m_Grn[i3 - 1] + m_Grn[i3 + 1]) >> 1);
                m_Grn[i4] = PixelMath.ByteClamp((m_Grn[i4 - 1] + m_Grn[i4 + 1]) >> 1);
                i1 += 2; i2 += 2; i3 += 2; i4 += 2;
            }

            // set startposition of nongreen pixels in the last four lines
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB)
            {
                i1 = (Height - 4) * Width + 2;
                i2 = (Height - 3) * Width + 1;
                i3 = (Height - 2) * Width + 2;
                i4 = (Height - 1) * Width + 1;
            }
            else {
                i1 = (Height - 4) * Width + 1;
                i2 = (Height - 3) * Width + 2;
                i3 = (Height - 2) * Width + 1;
                i4 = (Height - 1) * Width + 2;
            }
            for (x = 1; x < Width - 1; x += 2)
            {
                m_Grn[i1] = PixelMath.ByteClamp((m_Grn[i1 - 1] + m_Grn[i1 + 1]) >> 1);
                m_Grn[i2] = PixelMath.ByteClamp((m_Grn[i2 - 1] + m_Grn[i2 + 1]) >> 1);
                m_Grn[i3] = PixelMath.ByteClamp((m_Grn[i3 - 1] + m_Grn[i3 + 1]) >> 1);
                m_Grn[i4] = PixelMath.ByteClamp((m_Grn[i4 - 1] + m_Grn[i4 + 1]) >> 1);
                i1 += 2; i2 += 2; i3 += 2; i4 += 2;
            }

            //left and right border pixel
            i = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB)
            {
                for (y = 0; y < Height - 2; y += 2)
                {
                    m_Grn[i] = m_Grn[i + Width + 1] = PixelMath.ByteClamp((m_Grn[i + 1] + m_Grn[i + Width]) >> 1);
                    m_Grn[i + 2] = m_Grn[i + Width + 3] = PixelMath.ByteClamp((m_Grn[i + 3] + m_Grn[i + Width + 2]) >> 1);
                    i += Width - 4;

                    m_Grn[i] = m_Grn[i + Width + 1] = PixelMath.ByteClamp((m_Grn[i + 1] + m_Grn[i + Width]) >> 1);
                    m_Grn[i + 2] = m_Grn[i + Width + 3] = PixelMath.ByteClamp((m_Grn[i + 3] + m_Grn[i + Width + 2]) >> 1);
                    i += Width + 4;
                }
            }
            else {
                for (y = 0; y < Height - 2; y += 2)
                {
                    m_Grn[i + 1] = m_Grn[i + Width] = PixelMath.ByteClamp((m_Grn[i] + m_Grn[i + Width + 1]) >> 1);
                    m_Grn[i + 3] = m_Grn[i + Width + 2] = PixelMath.ByteClamp((m_Grn[i + 2] + m_Grn[i + Width + 3]) >> 1);
                    i += Width - 4;

                    m_Grn[i + 1] = m_Grn[i + Width] = PixelMath.ByteClamp((m_Grn[i] + m_Grn[i + Width + 1]) >> 1);
                    m_Grn[i + 3] = m_Grn[i + Width + 2] = PixelMath.ByteClamp((m_Grn[i + 2] + m_Grn[i + Width + 3]) >> 1);
                    i += Width + 4;
                }
            }
        }

        protected void InterpolateRedBlue()
        {
            int xMin = 1;
            int xMax = Width - 1;
            int yMin = 1;
            int yMax = Height - 1;
            int yMaxHalf = yMax / 2;

            switch (SensorType)
            {
                case SensorType.BG_GR:
                    Parallel.For(0, yMaxHalf, delegate (int y)
                    {
                        int linestart = (yMin + y * 2) * Width + xMin;
                        int i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Blu[i] = Eq23(i); i++;
                            m_Blu[i] = Eq20_21(i); m_Red[i] = Eq19_22(i); i++;
                        }
                        linestart += Width; i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Red[i] = Eq20_21(i); m_Blu[i] = Eq19_22(i); i++;
                            m_Red[i] = Eq23(i); i++;
                        }
                    });
                    break;
                case SensorType.RG_GB:
                    Parallel.For(0, yMaxHalf, delegate (int y)
                    {
                        int linestart = (yMin + y * 2) * Width + xMin;
                        int i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Red[i] = Eq23(i); i++;
                            m_Red[i] = Eq20_21(i); m_Blu[i] = Eq19_22(i); i++;
                        }
                        linestart += Width; i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Blu[i] = Eq20_21(i); m_Red[i] = Eq19_22(i); i++;
                            m_Blu[i] = Eq23(i); i++;
                        }
                    });
                    break;
                case SensorType.GB_RG:
                    Parallel.For(0, yMaxHalf, delegate (int y)
                    {
                        int linestart = (yMin + y * 2) * Width + xMin;
                        int i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Blu[i] = Eq20_21(i); m_Red[i] = Eq19_22(i); i++;
                            m_Blu[i] = Eq23(i); i++;
                        }
                        linestart += Width; i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Red[i] = Eq23(i); i++;
                            m_Red[i] = Eq20_21(i); m_Blu[i] = Eq19_22(i); i++;
                        }
                    });
                    break;
                case SensorType.GR_BG:
                    Parallel.For(0, yMaxHalf, delegate (int y)
                    {
                        int linestart = (yMin + y * 2) * Width + xMin;
                        int i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Red[i] = Eq20_21(i); m_Blu[i] = Eq19_22(i); i++;
                            m_Red[i] = Eq23(i); i++;
                        }
                        linestart += Width; i = linestart;
                        for (int x = xMin; x < xMax; x += 2)
                        {
                            m_Blu[i] = Eq23(i); i++;
                            m_Blu[i] = Eq20_21(i); m_Red[i] = Eq19_22(i); i++;
                        }
                    });
                    break;
            }
            //interpolate border pixel
            InterpolateRedBlueBorder();
        }

        protected void InterpolateRedBlueBorder()
        {
            //interpolate border pixel
            int xMax = Width - 1;
            int yMax = Height - 1;
            int width2 = 2 * Width;
            int i1 = 0;
            int i2 = (Height - 1) * Width + 1;

            switch (SensorType)
            {
                case SensorType.BG_GR:
                    // top and bottom pixel
                    for (int x = 0; x < Width - 2; x += 2)
                    {
                        m_Blu[i1 + 1] = PixelMath.ByteClamp((m_Blu[i1] + m_Blu[i1 + 2]) >> 1);
                        m_Red[i1] = m_Red[i1 + Width];
                        m_Red[i1 + 1] = m_Red[i1 + Width + 1];

                        m_Red[i2 + 1] = PixelMath.ByteClamp((m_Red[i2] + m_Red[i2 + 2]) >> 1);
                        m_Blu[i2] = m_Blu[i2 - Width];
                        m_Blu[i2 + 1] = m_Blu[i2 - Width + 1];

                        i1 += 2;
                        i2 += 2;
                    }

                    //left and right border pixel
                    i1 = 0;
                    i2 = Width - 1;
                    for (int y = 0; y < Height - 2; y += 2)
                    {
                        m_Blu[i1 + Width] = PixelMath.ByteClamp((m_Blu[i1] + m_Blu[i1 + width2]) >> 1);
                        m_Red[i1] = m_Red[i1 + 1];
                        m_Red[i1 + Width] = m_Red[i1 + Width + 1];

                        m_Red[i2 + Width] = PixelMath.ByteClamp((m_Red[i2] + m_Red[i2 + width2]) >> 1);
                        m_Blu[i2] = m_Blu[i2 - 1];
                        m_Blu[i2 + Width] = m_Blu[i2 + Width - 1];

                        i1 += width2;
                        i2 += width2;
                    }
                    break;
                case SensorType.RG_GB:
                    for (int x = 0; x < Width - 2; x += 2)
                    {
                        m_Red[i1 + 1] = PixelMath.ByteClamp((m_Red[i1] + m_Red[i1 + 2]) >> 1);
                        m_Blu[i1] = m_Blu[i1 + Width];
                        m_Blu[i1 + 1] = m_Blu[i1 + Width + 1];

                        m_Blu[i2 + 1] = PixelMath.ByteClamp((m_Blu[i2] + m_Blu[i2 + 2]) >> 1);
                        m_Red[i2] = m_Red[i2 - Width];
                        m_Red[i2 + 1] = m_Red[i2 - Width + 1];

                        i1 += 2;
                        i2 += 2;
                    }

                    //left and right border pixel
                    i1 = 0;
                    i2 = Width - 1;
                    for (int y = 0; y < Height - 2; y += 2)
                    {
                        m_Red[i1 + Width] = PixelMath.ByteClamp((m_Red[i1] + m_Red[i1 + width2]) >> 1);
                        m_Blu[i1] = m_Blu[i1 + 1];
                        m_Blu[i1 + Width] = m_Blu[i1 + Width + 1];

                        m_Blu[i2 + Width] = PixelMath.ByteClamp((m_Blu[i2] + m_Blu[i2 + width2]) >> 1);
                        m_Red[i2] = m_Red[i2 - 1];
                        m_Red[i2 + Width] = m_Red[i2 + Width - 1];

                        i1 += width2;
                        i2 += width2;
                    }
                    break;
                case SensorType.GB_RG:
                    for (int x = 0; x < Width - 2; x += 2)
                    {
                        m_Blu[i1] = m_Blu[i1 + 1];
                        m_Red[i1] = m_Red[i1 + Width];
                        m_Red[i1 + 1] = m_Red[i1 + Width + 1];

                        m_Red[i2 + 1] = PixelMath.ByteClamp((m_Red[i2] + m_Red[i2 + 2]) >> 1);
                        m_Blu[i2] = m_Blu[i2 - Width];
                        m_Blu[i2 + 1] = m_Blu[i2 - Width + 1];

                        i1 += 2;
                        i2 += 2;
                    }

                    //left and right border pixel
                    i1 = 0;
                    i2 = Width - 1;
                    for (int y = 0; y < Height - 2; y += 2)
                    {
                        m_Blu[i1 + Width] = PixelMath.ByteClamp((m_Blu[i1] + m_Blu[i1 + width2]) >> 1);
                        m_Red[i1] = m_Red[i1 + 1];
                        m_Red[i1 + Width] = m_Red[i1 + Width + 1];

                        m_Red[i2 + Width] = PixelMath.ByteClamp((m_Red[i2] + m_Red[i2 + width2]) >> 1);
                        m_Blu[i2] = m_Blu[i2 - 1];
                        m_Blu[i2 + Width] = m_Blu[i2 + Width - 1];

                        i1 += width2;
                        i2 += width2;
                    }
                    break;
                case SensorType.GR_BG:
                    for (int x = 0; x < Width - 2; x += 2)
                    {
                        m_Red[i1 + 1] = PixelMath.ByteClamp((m_Red[i1] + m_Red[i1 + 2]) >> 1);
                        m_Blu[i1] = m_Blu[i1 + Width];
                        m_Blu[i1 + 1] = m_Blu[i1 + Width + 1];

                        m_Blu[i2 + 1] = PixelMath.ByteClamp((m_Blu[i2] + m_Blu[i2 + 2]) >> 1);
                        m_Red[i2] = m_Red[i2 - Width];
                        m_Red[i2 + 1] = m_Red[i2 - Width + 1];

                        i1 += 2;
                        i2 += 2;
                    }

                    //left and right border pixel
                    i1 = 0;
                    i2 = Width - 1;
                    for (int y = 0; y < Height - 2; y += 2)
                    {
                        m_Red[i1 + Width] = PixelMath.ByteClamp((m_Red[i1] + m_Red[i1 + width2]) >> 1);
                        m_Blu[i1] = m_Blu[i1 + 1];
                        m_Blu[i1 + Width] = m_Blu[i1 + Width + 1];

                        m_Blu[i2 + Width] = PixelMath.ByteClamp((m_Blu[i2] + m_Blu[i2 + width2]) >> 1);
                        m_Red[i2] = m_Red[i2 - 1];
                        m_Red[i2 + Width] = m_Red[i2 + Width - 1];

                        i1 += width2;
                        i2 += width2;
                    }
                    break;
            }
        }

        protected void Refinement()
        {
            int x, y, i, offsetY;
            double K1, K2, K3, K4, a1, a2, a3, a4;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            // green
            int linestart = yMin * Width + xMin;
            if (offsetY == 0)
            {
                for (y = yMin; y < yMax; y += 2)
                {
                    i = linestart + offsetY;
                    // even lines
                    for (x = xMin + offsetY; x < xMax; x += 2)
                    {
                        K1 = m_Grn[i - Width] - m_Blu[i - Width];
                        K2 = m_Grn[i + 1] - m_Blu[i + 1];
                        K3 = m_Grn[i + Width] - m_Blu[i + Width];
                        K4 = m_Grn[i - 1] - m_Blu[i - 1];

                        a1 = 1 + Math.Abs(m_Blu[i - 2 * Width] - m_Blu[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a2 = 1 + Math.Abs(m_Blu[i + 2] - m_Blu[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);
                        a3 = 1 + Math.Abs(m_Blu[i + 2 * Width] - m_Blu[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a4 = 1 + Math.Abs(m_Blu[i - 2] - m_Blu[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);

                        m_Grn[i] = PixelMath.ByteClamp(m_Blu[i] + ((K1 / a1) + (K2 / a2) + (K3 / a3) + (K4 / a4)) / ((1 / a1) + (1 / a2) + (1 / a3) + (1 / a4)));
                        i += 2;
                    }

                    linestart += Width;
                    i = linestart + 1;
                    // odd lines
                    for (x = xMin + offsetY; x < xMax; x += 2)
                    {
                        K1 = m_Grn[i - Width] - m_Red[i - Width];
                        K2 = m_Grn[i + 1] - m_Red[i + 1];
                        K3 = m_Grn[i + Width] - m_Red[i + Width];
                        K4 = m_Grn[i - 1] - m_Red[i - 1];

                        a1 = 1 + Math.Abs(m_Red[i - 2 * Width] - m_Red[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a2 = 1 + Math.Abs(m_Red[i + 2] - m_Red[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);
                        a3 = 1 + Math.Abs(m_Red[i + 2 * Width] - m_Red[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a4 = 1 + Math.Abs(m_Red[i - 2] - m_Red[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);

                        m_Grn[i] = PixelMath.ByteClamp(m_Red[i] + ((K1 / a1) + (K2 / a2) + (K3 / a3) + (K4 / a4)) / ((1 / a1) + (1 / a2) + (1 / a3) + (1 / a4)));
                        i += 2;
                    }
                    linestart += Width;
                }
            }
            else {
                for (y = yMin; y < yMax; y += 2)
                {
                    i = linestart + offsetY;
                    // even lines
                    for (x = xMin + offsetY; x < xMax; x += 2)
                    {
                        K1 = m_Grn[i - Width] - m_Red[i - Width];
                        K2 = m_Grn[i + 1] - m_Red[i + 1];
                        K3 = m_Grn[i + Width] - m_Red[i + Width];
                        K4 = m_Grn[i - 1] - m_Red[i - 1];

                        a1 = 1 + Math.Abs(m_Red[i - 2 * Width] - m_Red[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a2 = 1 + Math.Abs(m_Red[i + 2] - m_Red[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);
                        a3 = 1 + Math.Abs(m_Red[i + 2 * Width] - m_Red[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a4 = 1 + Math.Abs(m_Red[i - 2] - m_Red[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);

                        m_Grn[i] = PixelMath.ByteClamp(m_Red[i] + ((K1 / a1) + (K2 / a2) + (K3 / a3) + (K4 / a4)) / ((1 / a1) + (1 / a2) + (1 / a3) + (1 / a4)));
                        i += 2;
                    }

                    linestart += Width;
                    i = linestart + 1;
                    // odd lines
                    for (x = xMin + offsetY; x < xMax; x += 2)
                    {
                        K1 = m_Grn[i - Width] - m_Blu[i - Width];
                        K2 = m_Grn[i + 1] - m_Blu[i + 1];
                        K3 = m_Grn[i + Width] - m_Blu[i + Width];
                        K4 = m_Grn[i - 1] - m_Blu[i - 1];

                        a1 = 1 + Math.Abs(m_Blu[i - 2 * Width] - m_Blu[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a2 = 1 + Math.Abs(m_Blu[i + 2] - m_Blu[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);
                        a3 = 1 + Math.Abs(m_Blu[i + 2 * Width] - m_Blu[i]) + Math.Abs(m_Grn[i - Width] - m_Grn[i + Width]);
                        a4 = 1 + Math.Abs(m_Blu[i - 2] - m_Blu[i]) + Math.Abs(m_Grn[i - 1] - m_Grn[i + 1]);

                        m_Grn[i] = PixelMath.ByteClamp(m_Blu[i] + ((K1 / a1) + (K2 / a2) + (K3 / a3) + (K4 / a4)) / ((1 / a1) + (1 / a2) + (1 / a3) + (1 / a4)));
                        i += 2;
                    }
                    linestart += Width;
                }
            }
        }

        protected void printData(ushort[] data)
        {
            int size = 9;
            Console.WriteLine("CPU:");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Console.Write("{0,4} ", data[y * Width + x]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        #endregion

        #region "Precalculations"
        protected void CalcColorDiff()
        {
            m_CdiffH = new int[FrameSize];
            m_CdiffV = new int[FrameSize];
            m_CdiffBh = new int[FrameSize];
            m_CdiffBv = new int[FrameSize];
            int x, y, i, offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;

            // alle nicht grünen Pixel
            int linestart = yMin * Width + xMin;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    m_CdiffH[i] = m_Raw[i] - m_Eq3[i];
                    m_CdiffV[i] = m_Raw[i] - m_Eq4[i];
                    m_CdiffBh[i] = m_Raw[i] - m_Eq5[i];
                    m_CdiffBv[i] = m_Raw[i] - m_Eq5[i];
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }

            // alle grünen Pixel
            linestart = yMin * Width + xMin;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 1; } else { offsetY = 0; }
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    m_CdiffH[i] = (m_CdiffH[i - 1] - m_CdiffH[i + 1]) >> 1;
                    m_CdiffV[i] = (m_CdiffV[i - Width] - m_CdiffV[i + Width]) >> 1;
                    m_CdiffBh[i] = (m_CdiffBh[i - 1] - m_CdiffBh[i + 1]) >> 1;
                    m_CdiffBv[i] = (m_CdiffBv[i - Width] - m_CdiffBv[i + Width]) >> 1;
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
        }

        protected void CalcEq3()
        {
            m_Eq3 = new byte[FrameSize];
            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(0, Height, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xStart; x < xMax; x += 2, i += 2)
                {
                    m_Eq3[i] = PixelMath.ByteClamp((-m_Raw[i - 2]
                                                   + 2 * m_Raw[i - 1]
                                                   + 2 * m_Raw[i]
                                                   + 2 * m_Raw[i + 1]
                                                   - m_Raw[i + 2]) >> 2);
                }
            });
        }

        protected void CalcEq4()
        {
            m_Eq4 = new byte[FrameSize];
            int offsetY;
            int yMin = 2;
            int yMax = Height - 2;
            int width2 = 2 * Width;

            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(yMin, yMax, delegate (int y)
            {
                int xStart = ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xStart; x < Width; x += 2, i += 2)
                {
                    m_Eq4[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                   + 2 * m_Raw[i - Width]
                                                   + 2 * m_Raw[i]
                                                   + 2 * m_Raw[i + Width]
                                                   - m_Raw[i + width2]) >> 2);
                }
            });
        }

        protected void CalcEq5()
        {
            m_Eq5 = new byte[FrameSize];
            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int width2 = 2 * Width;

            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(yMin, yMax, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xMin + offsetY; x < xMax; x += 2, i += 2)
                {
                    m_Eq5[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                   + 2 * m_Raw[i - Width]
                                                   - m_Raw[i - 2]
                                                   + 2 * m_Raw[i - 1]
                                                   + 4 * m_Raw[i]
                                                   + 2 * m_Raw[i + 1]
                                                   - m_Raw[i + 2]
                                                   + 2 * m_Raw[i + Width]
                                                   - m_Raw[i + width2]) >> 3);
                }
            });
        }

        protected void CalcEq6()
        {
            m_Eq6 = new double[FrameSize];
            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int[] mWidth = new int[5];

            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            for (int i = 0; i < mWidth.Length; i++)
            {
                mWidth[i] = (i - 2) * Width;
            }
            Parallel.For(yMin, yMax, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xMin + offsetY; x < xMax; x += 2, i += 2)
                {
                    m_Eq6[i] = 0;
                    for (int m = 0; m < 5; m++)
                    {
                        m_Eq6[i] += Math.Abs(m_Raw[i + mWidth[m] - 2] - m_Raw[i + mWidth[m]]) +
                                    Math.Abs(m_Raw[i + mWidth[m] - 1] - m_Raw[i + mWidth[m]]) +
                                    Math.Abs(m_Raw[i + mWidth[m] + 1] - m_Raw[i + mWidth[m]]) +
                                    Math.Abs(m_Raw[i + mWidth[m] + 2] - m_Raw[i + mWidth[m]]);
                    }
                }
            });
        }

        protected void CalcEq7()
        {
            m_Eq7 = new double[FrameSize];
            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int width2 = 2 * Width;

            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(yMin, yMax, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xMin + offsetY; x < xMax; x += 2, i += 2)
                {
                    m_Eq7[i] = 0;
                    for (int n = -2; n <= 2; n++)
                    {
                        m_Eq7[i] += Math.Abs(m_Raw[i - width2 + n] - m_Raw[i + n]) +
                                    Math.Abs(m_Raw[i - Width + n] - m_Raw[i + n]) +
                                    Math.Abs(m_Raw[i + Width + n] - m_Raw[i + n]) +
                                    Math.Abs(m_Raw[i + width2 + n] - m_Raw[i + n]);
                    }
                }
            });
        }

        protected void CalcEqE()
        {
            m_EqE = new double[FrameSize];
            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(0, Height, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xStart; x < xMax; x += 2, i += 2)
                {
                    m_EqE[i] = Math.Max(m_Eq7[i] / m_Eq6[i], m_Eq6[i] / m_Eq7[i]);
                }
            });
        }

        protected void CalcEq3_4_5_6_7_E()
        {
            m_Eq3 = new byte[FrameSize];
            m_Eq4 = new byte[FrameSize];
            m_Eq5 = new byte[FrameSize];
            m_Eq6 = new double[FrameSize];
            m_Eq7 = new double[FrameSize];
            m_EqE = new double[FrameSize];

            int offsetY;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int width2 = 2 * Width;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }
            int[] mWidth = new int[5];

            for (int i = 0; i < mWidth.Length; i++)
            {
                mWidth[i] = (i - 2) * Width;
            }

            Parallel.For(yMin, yMax, delegate (int y)
            {
                int xStart = xMin + ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xMin + offsetY; x < xMax; x += 2, i += 2)
                {
                    m_Eq3[i] = PixelMath.ByteClamp((-m_Raw[i - 2]
                                                   + 2 * m_Raw[i - 1]
                                                   + 2 * m_Raw[i]
                                                   + 2 * m_Raw[i + 1]
                                                   - m_Raw[i + 2]) >> 2);

                    m_Eq4[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                   + 2 * m_Raw[i - Width]
                                                   + 2 * m_Raw[i]
                                                   + 2 * m_Raw[i + Width]
                                                   - m_Raw[i + width2]) >> 2);

                    m_Eq5[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                   + 2 * m_Raw[i - Width]
                                                   - m_Raw[i - 2]
                                                   + 2 * m_Raw[i - 1]
                                                   + 4 * m_Raw[i]
                                                   + 2 * m_Raw[i + 1]
                                                   - m_Raw[i + 2]
                                                   + 2 * m_Raw[i + Width]
                                                   - m_Raw[i + width2]) >> 3);

                    float eq6 = 0;
                    float eq7 = 0;
                    for (int m = 0; m < 5; m++)
                    {
                        eq6 += Math.Abs(m_Raw[i + mWidth[m] - 2] - m_Raw[i + mWidth[m]]) +
                               Math.Abs(m_Raw[i + mWidth[m] - 1] - m_Raw[i + mWidth[m]]) +
                               Math.Abs(m_Raw[i + mWidth[m] + 1] - m_Raw[i + mWidth[m]]) +
                               Math.Abs(m_Raw[i + mWidth[m] + 2] - m_Raw[i + mWidth[m]]);
                    }

                    for (int n = -2; n <= 2; n++)
                    {
                        eq7 += Math.Abs(m_Raw[i - width2 + n] - m_Raw[i + n]) +
                               Math.Abs(m_Raw[i - Width + n] - m_Raw[i + n]) +
                               Math.Abs(m_Raw[i + Width + n] - m_Raw[i + n]) +
                               Math.Abs(m_Raw[i + width2 + n] - m_Raw[i + n]);
                    }
                    m_Eq6[i] = eq6;
                    m_Eq7[i] = eq7;
                    m_EqE[i] = Math.Max(eq7 / eq6, eq6 / eq7);

                }
            });
        }


        protected void CalcEq3_4_5()
        {
            m_Eq3 = new byte[FrameSize];
            m_Eq4 = new byte[FrameSize];
            m_Eq5 = new byte[FrameSize];

            int offsetY;
            int width2 = 2 * Width;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            Parallel.For(0, Height, delegate (int y)
            {
                int xStart = ((y & 1) == offsetY ? 0 : 1);
                int i = y * Width + xStart;
                for (int x = xStart; x < Width; x += 2, i += 2)
                {
                    if (y < 2 || y >= Height - 2 || x < 2 || x >= Width - 2)
                    {

                        m_Eq3[i] = PixelMath.ByteClamp((-m_Raw[CheckPixelIndex(x, y, -2, 0, i)]
                                                   + ((m_Raw[CheckPixelIndex(x, y, -1, 0, i)] +
                                                       m_Raw[i] +
                                                       m_Raw[CheckPixelIndex(x, y, 1, 0, i)]) << 1)
                                                   - m_Raw[CheckPixelIndex(x, y, 2, 0, i)]) >> 2);

                        m_Eq4[i] = PixelMath.ByteClamp((-m_Raw[CheckPixelIndex(x, y, 0, -2, i)]
                                               + ((m_Raw[CheckPixelIndex(x, y, 0, -1, i)] +
                                                   m_Raw[i] +
                                                   m_Raw[CheckPixelIndex(x, y, 0, 1, i)]) << 1)
                                               - m_Raw[CheckPixelIndex(x, y, 0, 2, i)]) >> 2);

                        m_Eq5[i] = PixelMath.ByteClamp((-m_Raw[CheckPixelIndex(x, y, 0, -2, i)]
                                               - m_Raw[CheckPixelIndex(x, y, -2, 0, i)]
                                               + ((m_Raw[CheckPixelIndex(x, y, 0, -1, i)] +
                                                   m_Raw[CheckPixelIndex(x, y, -1, 0, i)] +
                                                   m_Raw[CheckPixelIndex(x, y, 1, 0, i)] +
                                                   m_Raw[CheckPixelIndex(x, y, 0, 1, i)]) << 1)
                                               + (m_Raw[i] << 2)
                                               - m_Raw[CheckPixelIndex(x, y, 2, 0, i)]
                                               - m_Raw[CheckPixelIndex(x, y, 0, 2, i)]) >> 3);
                    }
                    else
                    {
                        m_Eq3[i] = PixelMath.ByteClamp((-m_Raw[i - 2]
                                                       + 2 * m_Raw[i - 1]
                                                       + 2 * m_Raw[i]
                                                       + 2 * m_Raw[i + 1]
                                                       - m_Raw[i + 2]) >> 2);

                        m_Eq4[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                       + 2 * m_Raw[i - Width]
                                                       + 2 * m_Raw[i]
                                                       + 2 * m_Raw[i + Width]
                                                       - m_Raw[i + width2]) >> 2);

                        m_Eq5[i] = PixelMath.ByteClamp((-m_Raw[i - width2]
                                                       + 2 * m_Raw[i - Width]
                                                       - m_Raw[i - 2]
                                                       + 2 * m_Raw[i - 1]
                                                       + 4 * m_Raw[i]
                                                       + 2 * m_Raw[i + 1]
                                                       - m_Raw[i + 2]
                                                       + 2 * m_Raw[i + Width]
                                                       - m_Raw[i + width2]) >> 3);
                    }

                }
            });
        }

        protected double CalcEq11(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term2, H2;
            double[] term1 = new double[9];

            term2 = 0.0;
            H2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term1[k + maskhalf] = Eq13_15(x, y, k);
                term2 += term1[k + maskhalf];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                H2 += (term1[n + maskhalf] - term2) * (term1[n + maskhalf] - term2);
            }
            H2 /= (2.0 * maskhalf + 1.0);
            return H2;
        }

        protected double CalcEq12(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term2, V2;
            double[] term1 = new double[9];
            term2 = 0.0;
            V2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term1[k + maskhalf] = Eq14_16(x, y, k);
                term2 += term1[k + maskhalf];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                V2 += (term1[n + maskhalf] - term2) * (term1[n + maskhalf] - term2);
            }
            V2 /= (2.0 * maskhalf + 1.0);
            return V2;
        }

        protected double CalcEq17(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term2, H2, V2, B2;
            double[] term1 = new double[9];
            term2 = 0.0;
            H2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term1[k + maskhalf] = Eq13_15b(x, y, k);
                term2 += term1[k + maskhalf];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                H2 += (term1[n + maskhalf] - term2) * (term1[n + maskhalf] - term2);
            }
            H2 /= (2.0 * maskhalf + 1.0);


            term2 = 0.0;
            V2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term1[k + maskhalf] = Eq14_16b(x, y, k);
                term2 += term1[k + maskhalf];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                V2 += (term1[n + maskhalf] - term2) * (term1[n + maskhalf] - term2);
            }
            V2 /= (2.0 * maskhalf + 1.0);
            B2 = (H2 + V2) / 2.0;
            return B2;
        }


        protected double CalcEq11simplified(int x, int y, int maskhalf, int pos)
        {
            double term2, H2;
            int[] term1 = new int[5];

            term1[0] = (m_Raw[pos - 4] - m_Grn[pos - 4]);
            term1[1] = (m_Raw[pos - 2] - m_Grn[pos - 2]);
            term1[2] = (m_Raw[pos] - m_Eq3[pos]);
            term1[3] = (m_Raw[pos + 2] - m_Eq3[pos + 2]);
            term1[4] = (m_Raw[pos + 4] - m_Eq3[pos + 4]);

            term2 = (term1[0] +
                  term1[1] +
                  term1[2] +
                  term1[3] +
                  term1[4]) / 5.0;

            H2 = (Math.Abs(term1[0] - term2) +
               Math.Abs(term1[1] - term2) +
               Math.Abs(term1[2] - term2) +
               Math.Abs(term1[3] - term2) +
               Math.Abs(term1[4] - term2)) / 5.0;

            return H2;
        }

        protected double CalcEq12simplified(int x, int y, int maskhalf, int pos)
        {
            double term2, V2;
            int[] term1 = new int[5];

            term1[0] = (m_Raw[pos - 4 * Width] - m_Grn[pos - 4 * Width]);
            term1[1] = (m_Raw[pos - 2 * Width] - m_Grn[pos - 2 * Width]);
            term1[2] = (m_Raw[pos] - m_Eq4[pos]);
            term1[3] = (m_Raw[pos + 2 * Width] - m_Eq4[pos + 2 * Width]);
            term1[4] = (m_Raw[pos + 4 * Width] - m_Eq4[pos + 4 * Width]);

            term2 = (term1[0] +
                  term1[1] +
                  term1[2] +
                  term1[3] +
                  term1[4]) / 5.0;

            V2 = (Math.Abs(term1[0] - term2) +
               Math.Abs(term1[1] - term2) +
               Math.Abs(term1[2] - term2) +
               Math.Abs(term1[3] - term2) +
               Math.Abs(term1[4] - term2)) / 5.0;
            return V2;
        }

        protected double CalcEq17simplified(int x, int y, int maskhalf, int pos)
        {
            double term2, H2, V2, B2;
            int[] term1 = new int[5];

            term1[0] = (m_Raw[pos - 4] - m_Grn[pos - 4]);
            term1[1] = (m_Raw[pos - 2] - m_Grn[pos - 2]);
            term1[2] = (m_Raw[pos] - m_Eq5[pos]);
            term1[3] = (m_Raw[pos + 2] - m_Eq5[pos + 2]);
            term1[4] = (m_Raw[pos + 4] - m_Eq5[pos + 4]);

            term2 = (term1[0] +
                  term1[1] +
                  term1[2] +
                  term1[3] +
                  term1[4]) / 5.0;

            H2 = (Math.Abs(term1[0] - term2) +
               Math.Abs(term1[1] - term2) +
               Math.Abs(term1[2] - term2) +
               Math.Abs(term1[3] - term2) +
               Math.Abs(term1[4] - term2)) / 5.0;


            term1[0] = (m_Raw[pos - 4 * Width] - m_Grn[pos - 4 * Width]);
            term1[1] = (m_Raw[pos - 2 * Width] - m_Grn[pos - 2 * Width]);
            term1[3] = (m_Raw[pos + 2 * Width] - m_Eq5[pos + 2 * Width]);
            term1[4] = (m_Raw[pos + 4 * Width] - m_Eq5[pos + 4 * Width]);

            term2 = (term1[0] +
                  term1[1] +
                  term1[2] +
                  term1[3] +
                  term1[4]) / 5.0;

            V2 = (Math.Abs(term1[0] - term2) +
               Math.Abs(term1[1] - term2) +
               Math.Abs(term1[2] - term2) +
               Math.Abs(term1[3] - term2) +
               Math.Abs(term1[4] - term2)) / 5.0;

            B2 = (H2 + V2) / 2.0;
            return B2;
        }


        protected double CalcEq11s(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term1, term2, H2;
            term2 = 0.0;
            H2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term2 += m_CdiffH[pos + k];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                term1 = m_CdiffH[pos + n];
                H2 += (term1 - term2) * (term1 - term2);
            }
            H2 /= (2.0 * maskhalf + 1.0);
            return H2;
        }

        protected double CalcEq12s(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term1, term2, V2;
            term2 = 0.0;
            V2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term2 += m_CdiffV[pos + k];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                term1 = m_CdiffV[pos + n];
                V2 += (term1 - term2) * (term1 - term2);
            }
            V2 /= (2.0 * maskhalf + 1.0);
            return V2;
        }

        protected double CalcEq17s(int x, int y, int maskhalf, int pos)
        {
            int n, k;
            double term1, term2, H2, V2, B2;
            term2 = 0.0;
            H2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term2 += m_CdiffBh[pos + k];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                term1 = m_CdiffBh[pos + n];
                H2 += (term1 - term2) * (term1 - term2);
            }
            H2 /= (2.0 * maskhalf + 1.0);


            term2 = 0.0;
            V2 = 0.0;
            for (k = -maskhalf; k <= maskhalf; k++)
            {
                term2 += m_CdiffBv[pos + k];
            }
            term2 /= (2.0 * maskhalf + 1.0);

            for (n = -maskhalf; n <= maskhalf; n++)
            {
                term1 = m_CdiffBv[pos + n];
                V2 += (term1 - term2) * (term1 - term2);
            }
            V2 /= (2.0 * maskhalf + 1.0);
            B2 = (H2 + V2) / 2.0;
            return B2;
        }


        protected int Eq13_15(int x, int y, int n)
        {
            int pos = y * Width + x + n;
            if (n == -2 || n == -4)
            {
                return (m_Raw[pos] - m_Grn[pos]);
            }
            else if (n == 0 || n == 2 || n == 4)
            {
                return (m_Raw[pos] - m_Eq3[pos]);
            }
            else // n == -1, 1, -3, 3
            {
                return (Eq13_15(x, y, n - 1) + Eq13_15(x, y, n + 1)) >> 1;
            }
        }

        protected int Eq14_16(int x, int y, int n)
        {
            int pos = (y + n) * Width + x;
            if (n == -2 || n == -4)
            {
                return (m_Raw[pos] - m_Grn[pos]);
            }
            else if (n == 0 || n == 2 || n == 4)
            {
                return (m_Raw[pos] - m_Eq4[pos]);
            }
            else // n == -1, 1, -3, 3
            {
                return (Eq14_16(x, y, n - 1) + Eq14_16(x, y, n + 1)) >> 1;
            }
        }

        protected int Eq13_15b(int x, int y, int n)
        {
            int pos = y * Width + x + n;
            if (n == -2 || n == -4)
            {
                return (m_Raw[pos] - m_Grn[pos]);
            }
            else if (n == 0 || n == 2 || n == 4)
            {
                return (m_Raw[pos] - m_Eq5[pos]);
            }
            else // n == -1, 1, -3, 3
            {
                return (Eq13_15b(x, y, n - 1) + Eq13_15b(x, y, n + 1)) >> 1;
            }
        }

        protected int Eq14_16b(int x, int y, int n)
        {
            int pos = (y + n) * Width + x;
            if (n == -2 || n == -4)
            {
                return (m_Raw[pos] - m_Grn[pos]);
            }
            else if (n == 0 || n == 2 || n == 4)
            {
                return (m_Raw[pos] - m_Eq5[pos]);
            }
            else // n == -1, 1, -3, 3
            {
                return (Eq14_16b(x, y, n - 1) + Eq14_16b(x, y, n + 1)) >> 1;
            }
        }


        protected byte Eq19_22(int pos)
        {
            return PixelMath.ByteClamp(m_Grn[pos] + ((m_Raw[pos - 1] - m_Grn[pos - 1] + m_Raw[pos + 1] - m_Grn[pos + 1]) >> 1));
        }

        protected byte Eq20_21(int pos)
        {
            return PixelMath.ByteClamp(m_Grn[pos] + ((m_Raw[pos - Width] - m_Grn[pos - Width] + m_Raw[pos + Width] - m_Grn[pos + Width]) >> 1));
        }

        protected byte Eq23(int pos)
        {
            return PixelMath.ByteClamp(m_Grn[pos] + (((m_Raw[pos - Width - 1] - m_Grn[pos - Width - 1]) +
                                                    (m_Raw[pos - Width + 1] - m_Grn[pos - Width + 1]) +
                                                    (m_Raw[pos + Width - 1] - m_Grn[pos + Width - 1]) +
                                                    (m_Raw[pos + Width + 1] - m_Grn[pos + Width + 1])) >> 2));
        }


        protected byte Eq19_22(int x, int y, int pos)
        {
            int posA = (x - 1 < 0) ? pos + 1 : pos - 1;
            int posB = (x + 1 >= Width) ? pos - 1 : pos + 1;

            return PixelMath.ByteClamp(m_Grn[pos] + ((m_Raw[posA] - m_Grn[posA] + m_Raw[posB] - m_Grn[posB]) >> 1));
        }

        protected byte Eq20_21(int x, int y, int pos)
        {
            int posA = (y - 1 < 0) ? pos + Width : pos - Width;
            int posB = (y + 1 >= Height) ? pos - Width : pos + Width;

            return PixelMath.ByteClamp(m_Grn[pos] + ((m_Raw[posA] - m_Grn[posA] + m_Raw[posB] - m_Grn[posB]) >> 1));
        }

        protected byte Eq23(int x, int y, int pos)
        {
            int posA = CheckPixelIndex(x, y, -1, -1, pos);
            int posB = CheckPixelIndex(x, y, 1, -1, pos);
            int posC = CheckPixelIndex(x, y, -1, 1, pos);
            int posD = CheckPixelIndex(x, y, -1, -1, pos);

            return PixelMath.ByteClamp(m_Grn[pos] + (((m_Raw[posA] - m_Grn[posA]) +
                                                (m_Raw[posB] - m_Grn[posB]) +
                                                (m_Raw[posC] - m_Grn[posC]) +
                                                (m_Raw[posD] - m_Grn[posD])) >> 2));
        }


        #endregion
    }

    /// <summary>
    /// Original VCD bayeralgorithm from this Paper:
    /// Color Demosaicing Using Variance of Color Differences, King-Hong Chung and Yuk-Hee Chan
    /// </summary>
    public class BayerVCD : BayerAlgorithm
    {
        public BayerVCD(RawImage raw) : base(raw, "VCD") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3_4_5_6_7_E();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            double H2, V2, B2;
            int x, i;
            int maskhalf = 4;
            int xMin = maskhalf;
            int xMax = Width - maskhalf;
            int yMin = maskhalf;
            int yMax = Height - maskhalf;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }
            int linestart = yMin * Width + xMin;

            //double minH2 = double.MaxValue;
            //double minV2 = double.MaxValue;
            //double minB2 = double.MaxValue;
            //double maxH2 = double.MinValue;
            //double maxV2 = double.MinValue;
            //double maxB2 = double.MinValue;
            for (int y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    //m_Grn[i] = m_Eq5[i];

                    //H2 = CalcEq11(x, y, maskhalf, i);
                    //V2 = CalcEq12(x, y, maskhalf, i);
                    //B2 = CalcEq17(x, y, maskhalf, i);

                    //m_Grn[i] = (ushort)(H2 / 128);
                    //m_Blu[i] = (ushort)(V2 / 128);
                    //m_Red[i] = (ushort)(B2 / 128);

                    //if (H2 > maxH2) { maxH2 = H2; }
                    //if (V2 > maxV2) { maxV2 = V2; }
                    //if (B2 > maxB2) { maxB2 = B2; }
                    //if (H2 < minH2) { minH2 = H2; }
                    //if (V2 < minV2) { minV2 = V2; }
                    //if (B2 < minB2) { minB2 = B2; }

                    if (m_EqE[i] > m_threshold) //its a sharp block
                    {
                        if (m_Eq6[i] < m_Eq7[i]) { m_Grn[i] = m_Eq3[i]; } else { m_Grn[i] = m_Eq4[i]; }
                    }
                    else //calculate variances in the 9x9 block
                    {
                        H2 = CalcEq11(x, y, maskhalf, i);
                        V2 = CalcEq12(x, y, maskhalf, i);
                        B2 = CalcEq17(x, y, maskhalf, i);
                        if (H2 <= V2)
                        {
                            if (H2 <= B2) { m_Grn[i] = m_Eq3[i]; } else { m_Grn[i] = m_Eq5[i]; }
                        }
                        else
                        {
                            if (V2 <= B2) { m_Grn[i] = m_Eq4[i]; } else { m_Grn[i] = m_Eq5[i]; }
                        }
                    }
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }


    /// <summary>
    /// A simple Bayeralgorithm. The missing green pixel is the average from the four neighbours
    /// </summary>
    public class BayerSimple : BayerAlgorithm
    {
        public BayerSimple(RawImage raw) : base(raw, "Simple") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int x, y, i;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    m_Grn[i] = PixelMath.ByteClamp((m_Raw[i - Width]
                                           + m_Raw[i - 1]
                                           + m_Raw[i + 1]
                                           + m_Raw[i + Width]) >> 2);
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }

    /// <summary>
    /// ACPI Bayeralgorithm
    /// </summary>
    public class BayerACPI : BayerAlgorithm
    {
        public BayerACPI(RawImage raw) : base(raw, "ACPI") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int x, y, i, dH, dV;
            int xMin = 2;
            int xMax = Width - 2;
            int yMin = 2;
            int yMax = Height - 2;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    dH = Math.Abs(m_Raw[i - 1] - m_Raw[i + 1]) +
                    Math.Abs(2 * m_Raw[i] - m_Raw[i - 2] - m_Raw[i + 2]);

                    dV = Math.Abs(m_Raw[i - Width] - m_Raw[i + Width]) +
                    Math.Abs(2 * m_Raw[i] - m_Raw[i - 2 * Width] - m_Raw[i + 2 * Width]);

                    if (dH < dV)
                    {
                        m_Grn[i] = PixelMath.ByteClamp((-m_Raw[i - 2]
                                              + 2 * m_Raw[i - 1]
                                              + 2 * m_Raw[i]
                                              + 2 * m_Raw[i + 1]
                                              - m_Raw[i + 2]) >> 2);
                    }
                    else if (dH > dV)
                    {
                        m_Grn[i] = PixelMath.ByteClamp((-m_Raw[i - 2 * Width]
                                              + 2 * m_Raw[i - Width]
                                              + 2 * m_Raw[i]
                                              + 2 * m_Raw[i + Width]
                                              - m_Raw[i + 2 * Width]) >> 2);
                    }
                    else {
                        m_Grn[i] = PixelMath.ByteClamp((-m_Raw[i - 2 * Width]
                                              + 2 * m_Raw[i - Width]
                                              - m_Raw[i - 2]
                                              + 2 * m_Raw[i - 1]
                                              + 4 * m_Raw[i]
                                              + 2 * m_Raw[i + 1]
                                              - m_Raw[i + 2]
                                              + 2 * m_Raw[i + Width]
                                              - m_Raw[i + 2 * Width]) >> 3);
                    }
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();

        }
    }

    /// <summary>
    /// Bayeralgorithm that is implemented in the DSP
    /// massive simplified version of the VCD algorithm
    /// </summary>
    public class BayerDSP : BayerAlgorithm
    {
        public BayerDSP(RawImage raw) : base(raw, "DSP") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int x, y, i;
            int offsetY = 0;
            int linestart = 0;

            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            for (y = 0; y < Height; y++)
            {
                i = linestart + offsetY;
                for (x = offsetY; x < Width; x += 2)
                {
                    m_Grn[i] = m_Eq3[i];
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }

    /// <summary>
    /// simplified version from the VCD algorithm (described in the original VCD-paper)
    /// </summary>
    public class BayerVCDsimple : BayerAlgorithm
    {
        public BayerVCDsimple(RawImage raw) : base(raw, "VCDsimple") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3();
            CalcEq4();
            CalcEq5();
            CalcEq6();
            CalcEq7();
            CalcEqE();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int countA, countB, countC, countD, countE;
            double H2, V2, B2;
            int x, y, i;
            int maskhalf = 4;
            int xMin = maskhalf;
            int xMax = Width - maskhalf;
            int yMin = maskhalf;
            int yMax = Height - maskhalf;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            countA = countB = countC = countD = countE = 0;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    if (m_EqE[i] > m_threshold) //its a sharp block
                    {
                        if (m_Eq6[i] < m_Eq7[i]) { m_Grn[i] = m_Eq3[i]; countA++; } else { m_Grn[i] = m_Eq4[i]; countB++; }
                    }
                    else //calculate variances in the 9x9 block
                    {
                        H2 = CalcEq11simplified(x, y, maskhalf, i);
                        V2 = CalcEq12simplified(x, y, maskhalf, i);
                        B2 = CalcEq17simplified(x, y, maskhalf, i);

                        if (H2 <= V2)
                        {
                            if (H2 <= B2) { m_Grn[i] = m_Eq3[i]; countC++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                        else
                        {
                            if (V2 <= B2) { m_Grn[i] = m_Eq4[i]; countD++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                    }
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }

    /// <summary>
    /// simplified version from the VCD algorithm
    /// </summary>
    public class BayerVCDcolorDiff : BayerAlgorithm
    {
        public BayerVCDcolorDiff(RawImage raw) : base(raw, "VCDcolorDiff") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3();
            CalcEq4();
            CalcEq5();
            CalcEq6();
            CalcEq7();
            CalcEqE();
            CalcColorDiff();
            InterpolateGreen();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            double H2, V2, B2;
            int x, y, i;
            int maskhalf = 4;
            int xMin = maskhalf;
            int xMax = Width - maskhalf;
            int yMin = maskhalf;
            int yMax = Height - maskhalf;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    if (m_EqE[i] > m_threshold) //its a sharp block
                    {
                        if (m_Eq6[i] < m_Eq7[i]) { m_Grn[i] = m_Eq3[i]; } else { m_Grn[i] = m_Eq4[i]; }
                    }
                    else //calculate variances in the 9x9 block
                    {
                        H2 = CalcEq11s(x, y, maskhalf, i);
                        V2 = CalcEq12s(x, y, maskhalf, i);
                        B2 = CalcEq17s(x, y, maskhalf, i);

                        if (H2 <= V2)
                        {
                            if (H2 <= B2) { m_Grn[i] = m_Eq3[i]; } else { m_Grn[i] = m_Eq5[i]; }
                        }
                        else
                        {
                            if (V2 <= B2) { m_Grn[i] = m_Eq4[i]; } else { m_Grn[i] = m_Eq5[i]; }
                        }
                    }
                    //update colordiff
                    m_CdiffH[i] = m_Raw[i] - m_Grn[i];
                    m_CdiffV[i] = m_Raw[i] - m_Grn[i];
                    m_CdiffBh[i] = m_Raw[i] - m_Grn[i];
                    m_CdiffBv[i] = m_Raw[i] - m_Grn[i];

                    m_CdiffH[i - 1] = (m_CdiffH[i - 2] - m_CdiffH[i]) >> 1;
                    m_CdiffV[i - Width] = (m_CdiffV[i - 2 * Width] - m_CdiffV[i]) >> 1;
                    m_CdiffBh[i - 1] = (m_CdiffBh[i - 2] - m_CdiffBh[i]) >> 1;
                    m_CdiffBv[i - Width] = (m_CdiffBv[i - 2 * Width] - m_CdiffBv[i]) >> 1;

                    m_CdiffH[i + 1] = (m_CdiffH[i] - m_CdiffH[i + 2]) >> 1;
                    m_CdiffV[i + Width] = (m_CdiffV[i] - m_CdiffV[i + 2 * Width]) >> 1;
                    m_CdiffBh[i + 1] = (m_CdiffBh[i] - m_CdiffBh[i + 2]) >> 1;
                    m_CdiffBv[i + Width] = (m_CdiffBv[i] - m_CdiffBv[i + 2 * Width]) >> 1;

                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }

    /// <summary>
    /// VCD algorithm with refinement step (described in the original VCD-paper)
    /// </summary>
    public class BayerVCDrefinement : BayerAlgorithm
    {
        public BayerVCDrefinement(RawImage raw) : base(raw, "VCDrefinement") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3();
            CalcEq4();
            CalcEq5();
            CalcEq6();
            CalcEq7();
            CalcEqE();
            InterpolateGreen();
            InterpolateRedBlue();
            Refinement();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int countA, countB, countC, countD, countE;
            double H2, V2, B2;
            int x, y, i;
            int maskhalf = 4;
            int xMin = maskhalf;
            int xMax = Width - maskhalf;
            int yMin = maskhalf;
            int yMax = Height - maskhalf;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            countA = countB = countC = countD = countE = 0;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    if (m_EqE[i] > m_threshold) //its a sharp block
                    {
                        if (m_Eq6[i] < m_Eq7[i]) { m_Grn[i] = m_Eq3[i]; countA++; } else { m_Grn[i] = m_Eq4[i]; countB++; }
                    }
                    else //calculate variances in the 9x9 block
                    {
                        H2 = CalcEq11(x, y, maskhalf, i);
                        V2 = CalcEq12(x, y, maskhalf, i);
                        B2 = CalcEq17(x, y, maskhalf, i);

                        if (H2 <= V2)
                        {
                            if (H2 <= B2) { m_Grn[i] = m_Eq3[i]; countC++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                        else
                        {
                            if (V2 <= B2) { m_Grn[i] = m_Eq4[i]; countD++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                    }
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }

            InterpolateGreenBorderPixel();
            // finished green interpolation
        }
    }

    /// <summary>
    /// simplified VCD algorithm with refinement step (described in the original VCD-paper)
    /// </summary>
    public class BayerVCDsimpleAndRefinement : BayerAlgorithm
    {
        public BayerVCDsimpleAndRefinement(RawImage raw) : base(raw, "VCDsimpleAndRefinement") { }

        public override void Convert(WriteableBitmap bitmap)
        {
            CopyRawToRGB();
            CalcEq3();
            CalcEq4();
            CalcEq5();
            CalcEq6();
            CalcEq7();
            CalcEqE();
            InterpolateGreen();
            InterpolateRedBlue();
            Refinement();
            InterpolateRedBlue();
            CopyRGBtoBitmap(bitmap);
        }

        protected override void InterpolateGreen()
        {
            int countA, countB, countC, countD, countE;
            double H2, V2, B2;
            int x, y, i;
            int maskhalf = 4;
            int xMin = maskhalf;
            int xMax = Width - maskhalf;
            int yMin = maskhalf;
            int yMax = Height - maskhalf;
            int offsetY = 0;
            if (SensorType == SensorType.BG_GR || SensorType == SensorType.RG_GB) { offsetY = 0; } else { offsetY = 1; }

            int linestart = yMin * Width + xMin;
            countA = countB = countC = countD = countE = 0;
            for (y = yMin; y < yMax; y++)
            {
                i = linestart + offsetY;
                for (x = xMin + offsetY; x < xMax; x += 2)
                {
                    if (m_EqE[i] > m_threshold) //its a sharp block
                    {
                        if (m_Eq6[i] < m_Eq7[i]) { m_Grn[i] = m_Eq3[i]; countA++; } else { m_Grn[i] = m_Eq4[i]; countB++; }
                    }
                    else //calculate variances in the 9x9 block
                    {
                        H2 = CalcEq11simplified(x, y, maskhalf, i);
                        V2 = CalcEq12simplified(x, y, maskhalf, i);
                        B2 = CalcEq17simplified(x, y, maskhalf, i);

                        if (H2 <= V2)
                        {
                            if (H2 <= B2) { m_Grn[i] = m_Eq3[i]; countC++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                        else
                        {
                            if (V2 <= B2) { m_Grn[i] = m_Eq4[i]; countD++; } else { m_Grn[i] = m_Eq5[i]; countE++; }
                        }
                    }
                    i += 2;
                }
                offsetY = 1 - offsetY;
                linestart += Width;
            }
            InterpolateGreenBorderPixel();
        }
    }

}