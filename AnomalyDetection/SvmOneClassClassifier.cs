using Emgu.CV;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyDetection
{
    // TODO save / load
    /// <summary>
    /// [1] http://stackoverflow.com/questions/32099618/emgu-cv-svm-example-not-working-on-version-3-0-0
    /// [2] http://www.emgu.com/wiki/index.php/SVM_(Support_Vector_Machine)_in_CSharp
    /// [3] http://stackoverflow.com/questions/18518852/every-time-getting-positive-result-while-predicting-from-svm
    /// </summary>
    public class SvmOneClassClassifier
    {
        private SVM Model;

        /// <summary>
        /// Fit an OCC SVM to the data contained in X.
        /// </summary>
        /// <param name="X">Rows = samples, columns = features</param>
        public void Fit(Mat X, double gamma, double nu)
        {
            Console.WriteLine($"[{DateTime.Now}] SvmOneClassClassifier.Fit: fitting SVM");

            Model = new SVM();
            Model.Type = SVM.SvmType.OneClass;
            Model.SetKernel(SVM.SvmKernelType.Rbf);

            // Irrelevant for one-class
            Model.C = 1;

            Model.Coef0 = 0;
            Model.Degree = 3;

            // see paper - where?
            //Model.Gamma = 1 / (2 * Math.Exp(-2.5));

            // sklearn: 1 / n_features
            //Model.Gamma = 1 / ((double)(X.Cols));

            Model.Gamma = gamma;

            // Small value = few samples will be classified as outliers
            // Big value = a lot of samples will be classified as outliers
            Model.Nu = nu;

            // epsilon
            // Irrelevant for one-class
            Model.P = 0;

            Model.TermCriteria = new MCvTermCriteria(100, 0.00001);

            var trainData = new TrainData(X, DataLayoutType.RowSample, new Mat());
            bool trained = Model.Train(trainData);
            Console.WriteLine($"trained={trained}");
        }

        /// <summary>
        /// Predicts a label for each sample using the fitted SVM model.
        /// One sample describes a part of the rope which is as wide as its diameter and CellWidth pixels high.
        /// </summary>
        /// <param name="X">Feature matrix nSamples x nFeatures</param>
        /// <returns>Labels per sample: 1 = normal rope, 0 = anomaly</returns>
        public int[] Predict(Mat X)
        {
            Console.WriteLine($"[{DateTime.Now}] SvmOneClassClassifier.Predict: predicting labels with SVM");
            var resultMatrix = new Mat();
            Model.Predict(X, resultMatrix);

            Debug.Assert(resultMatrix.Cols == 1);
            float[] result = new float[resultMatrix.Rows];
            Marshal.Copy(resultMatrix.DataPointer, result, 0, result.Length);

            return result.Select(f => (int)(Math.Round(f))).ToArray();
        }

        public void Save(string pathToModelFile)
        {
            Model.Save(pathToModelFile);
        }

        public void Load(string pathToModelFile)
        {
            var fileNode = new FileStorage(pathToModelFile, FileStorage.Mode.Read).GetFirstTopLevelNode();
            Model = new SVM();
            Model.Read(fileNode);
        }
    }
}
