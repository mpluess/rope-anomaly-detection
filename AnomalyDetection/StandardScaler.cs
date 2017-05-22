using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyDetection
{
    public class StandardScaler
    {
        private float[] means = null;
        private float[] stdDevs = null;

        public void Fit(Mat X)
        {
            means = new float[X.Cols];
            stdDevs = new float[X.Cols];
            float[] data = new float[X.Rows * X.Cols];
            Marshal.Copy(X.DataPointer, data, 0, data.Length);
            for (int x = 0; x < X.Cols; ++x)
            {
                float[] colData = new float[X.Rows];
                for (int y = 0; y < X.Rows; ++y)
                {
                    colData[y] = data[y * X.Cols + x];
                }
                means[x] = colData.Average();
                stdDevs[x] = colData.StdDev();
            }
        }

        public Mat Transform(Mat X)
        {
            if (means == null || stdDevs == null)
            {
                throw new InvalidOperationException("Please call Fit before Transform");
            }
            var matrix = new Matrix<float>(X.Rows, X.Cols, X.NumberOfChannels);
            X.CopyTo(matrix);
            
            for (int y = 0; y < matrix.Rows; ++y)
            {
                for (int x = 0; x < matrix.Cols; ++x)
                {
                    matrix[y, x] = (matrix[y, x] - means[x]) / stdDevs[x];
                }
            }

            return matrix.Mat.Clone();
        }
    }
}
