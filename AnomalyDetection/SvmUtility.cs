using System;
using System.Drawing;
using RawLibrary;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.IO;

namespace AnomalyDetection
{
    /// <summary>
    /// This class contains static methods used for the training and analysis of rope images.
    /// </summary>
    public class SvmUtility
    {
        /// <summary>
        /// Converts a RawImage to Mat.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Mat RawToMat(RawImage image)
        {
            Mat temp = new Mat(new Size(image.ImageWidth, image.ImageHeight), DepthType.Cv32S, 3);
            var im = temp.ToImage<Gray, byte>();

            int sourceIndex = 0;
            for (int y = 0; y < image.ImageHeight; ++y)
            {
                for (int x = 0; x < image.ImageWidth; ++x)
                {
                    im[y, x] = new Gray(image.Raw.Data[sourceIndex]); // double values from 0.0 to 255.0
                    sourceIndex++;
                }
            }
            return im.Mat.Clone();
        }

        /// <summary>
        /// Converts the train data to fit as input for SVM.
        /// 
        /// Convert training/testing set to be used by OpenCV Machine Learning algorithms.
        /// TrainData is a matrix of size(#samples x max(#cols,#rows) per samples), in 32FC1.
        /// Transposition of samples are made if needed.
        /// 
        /// Rewritten from: [source] TODO
        /// </summary>
        /// <param name="trainSamples"></param>
        /// <returns></returns>
        public static Mat ConvertToMl(VectorOfMat trainSamples)
        {
            int rows = trainSamples.Size;
            int cols = Math.Max(trainSamples[0].Cols, trainSamples[0].Rows);

            Mat trainData = new Mat(new Size(cols, rows), DepthType.Cv32F, 1);

            for (int i = 0; i < trainSamples.Size; ++i)
            {
                Mat currentSample = trainSamples[i];
                Mat row = new Mat(trainData, new Rectangle(new Point(0, i), new Size(trainData.Size.Width, 1)));
                currentSample.CopyTo(row);
            }
            return trainData;
        }
    }
}
