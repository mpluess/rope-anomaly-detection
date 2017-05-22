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
    /// <summary>
    /// TODO SVM = unmanaged, memory leak possible? Mat in general?
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

            // Irrelevant for OpenCV's one-class implementation (NuSVM)
            Model.C = 1;

            Model.Coef0 = 0;
            Model.Degree = 3;

            // Usual range for RBF kernel: [10^-3, 10^3]
            Model.Gamma = gamma;

            // Small value = few samples will be classified as outliers
            // Big value = a lot of samples will be classified as outliers
            // Range: ]0, 1[
            Model.Nu = nu;

            // epsilon
            // Irrelevant for one-class
            Model.P = 0;

            // OpenCV / EmguCV doesn't warn you when the optimization has not yet converged after maxIteration,
            // so better set this high enough! The flag returned by Train() doesn't give any clues about convergence either.
            Model.TermCriteria = new MCvTermCriteria(10000, 0.00001);

            var trainData = new TrainData(X, DataLayoutType.RowSample, new Mat());
            Model.Train(trainData);
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
