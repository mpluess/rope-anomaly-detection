using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace RawLibrary
{
   public class PixelMath
   {
      /// <summary>
      /// assure that the value "a" is between 0 and 255
      /// </summary>
      /// <param name="a">value to check</param>
      /// <returns>value a between 0 and 255</returns>
      public static Byte ByteClamp(double a)
      {
         return (((uint)a & 0xffffff00) == 0) ? (Byte)a : ((a < 0) ? (Byte)0 : (Byte)255);
      }

      /// <summary>
      /// assure that the value "a" is between 0 and 1023 (10bit)
      /// </summary>
      /// <param name="a"></param>
      /// <returns></returns>
      public static ushort Clamp10Bit(double a)
      {
         return (((uint)a & 0xfffffc00) == 0) ? (ushort)a : ((a < 0) ? (ushort)0 : (ushort)1023);
      }

      public static ushort Clamp8Bit(double a)
      {
         return (((uint)a & 0xffffff00) == 0) ? (ushort)a : ((a < 0) ? (ushort)0 : (ushort)255);
      }


      public static ushort BitClamp(double a, int bits)
      {
         if (bits == 10)
            return Clamp10Bit(a);
         else
            return Clamp8Bit(a);
      }

      /// <summary>
      /// sorts an array an return the medianvalue
      /// </summary>
      /// <param name="data">array to sort</param>
      /// <returns>median</returns>
      public static int Combsort(int[] data)
      {
         int amount = data.Length;
         int gap = amount;
         bool swapped = false;
         while (gap > 1 || swapped)
         {
            //shrink factor 1.3
            gap = gap >> 1;//(gap * 10) / 16;
            if (gap == 9 || gap == 10) gap = 11;
            if (gap < 1) gap = 1;
            swapped = false;
            for (int i = 0; i < amount - gap; i++)
            {
               int j = i + gap;
               if (data[i] > data[j])
               {
                  data[i] += data[j];
                  data[j] = data[i] - data[j];
                  data[i] -= data[j];
                  swapped = true;
               }
            }
         }
         return data[(int)Math.Ceiling( (amount - 1) / 2.0)];
      }
      

      /// <summary>
      /// Calculate Color Peak Signal Noise Ratio
      /// </summary>
      /// <param name="Iorigin">original image</param>
      /// <param name="Ireconstructed">reconstructed image</param>
      /// <param name="border">borderpixel are ignored</param>
      /// <returns></returns>
      public static double CalcCPSNR(Bitmap Iorigin, Bitmap Ireconstructed, int border)
      {
         BitmapData BmdO = Iorigin.LockBits(new Rectangle(0, 0, Iorigin.Width, Iorigin.Height), ImageLockMode.ReadOnly, Iorigin.PixelFormat);
         BitmapData BmdR = Ireconstructed.LockBits(new Rectangle(0, 0, Ireconstructed.Width, Ireconstructed.Height), ImageLockMode.ReadOnly, Ireconstructed.PixelFormat);
         int BpPO = Image.GetPixelFormatSize(Iorigin.PixelFormat) / 8;
         int BpPR = Image.GetPixelFormatSize(Ireconstructed.PixelFormat) / 8;
         int strideO = BmdO.Stride;
         int strideR = BmdR.Stride;
         int x, y, i, j, linestartO, linestartR;
         double CMSE = 0;
         double CPSNR;
         int xMin = border;
         int xMax = Iorigin.Width - border;
         int yMin = border;
         int yMax = Iorigin.Height - border;

         unsafe
         {
            Byte* bgrO = (Byte*)BmdO.Scan0.ToPointer();
            Byte* bgrR = (Byte*)BmdR.Scan0.ToPointer();
            linestartO = yMin * strideO + xMin * BpPO;
            linestartR = yMin * strideR + xMin * BpPR;
            for (y = yMin; y < yMax; y++)
            {
               i = linestartO;
               j = linestartR;
               for (x = xMin; x < xMax; x++)
               {
                  CMSE += ((bgrO[i] - bgrR[j]) * (bgrO[i] - bgrR[j]));
                  CMSE += ((bgrO[i + 1] - bgrR[j + 1]) * (bgrO[i + 1] - bgrR[j + 1]));
                  CMSE += ((bgrO[i + 2] - bgrR[j + 2]) * (bgrO[i + 2] - bgrR[j + 2]));
                  i += BpPO;
                  j += BpPR;
               }
               linestartO += strideO;
               linestartR += strideR;
            }
            CMSE /= (3.0 * (xMax - xMin) * (yMax - yMin));
            CPSNR = 10 * Math.Log10((255 * 255) / CMSE);
         }
         Iorigin.UnlockBits(BmdO);
         Ireconstructed.UnlockBits(BmdR);
         return CPSNR;
      }
   }

   public class cPrecisionTimer
   {
      [DllImport("Kernel32.dll")]
      private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
      [DllImport("Kernel32.dll")]
      private static extern bool QueryPerformanceFrequency(out long lpFrequency);

      private  long     m_TimerStart;
      private  long     m_TimerEnd;
      public   long     Frequency;
      public   double   Duration; //Time in milliseconds

      public cPrecisionTimer()
      {  
         m_TimerStart = 0;
         m_TimerEnd   = 0;

         if (QueryPerformanceFrequency(out Frequency) == false)
         {
            throw new Win32Exception();
         }
      }

      public void Start()
      {
         Thread.Sleep(0);
         QueryPerformanceCounter(out m_TimerStart);
      }

      public void Stop()
      {
         QueryPerformanceCounter(out m_TimerEnd);
         Duration = 1000 * (double)(m_TimerEnd - m_TimerStart) / (double)Frequency;
      }
   }
}
