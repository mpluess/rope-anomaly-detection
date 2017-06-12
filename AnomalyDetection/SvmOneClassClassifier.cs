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

            // Irrelevant for OpenCV's one-class implementation (uses nu, not C)
            Model.C = 1;

            Model.Coef0 = 0;
            Model.Degree = 3;

            // gamma defines how much influence a single training example has. The larger gamma is, the closer other examples must be to be affected.
            // Source: http://scikit-learn.org/stable/modules/svm.html
            //
            // Usual range for RBF kernel: [10^-3, 10^3]
            // Source: http://scikit-learn.org/stable/auto_examples/svm/plot_rbf_parameters.html
            Model.Gamma = gamma;

            // The parameter nu, also known as the margin of the One-Class SVM, corresponds to the probability of finding a new, but regular, observation outside the frontier.
            // Suppose you are given some dataset drawn from an underlying probability distribution P
            // and you want to estimate a "simple" subset S of input space such that the probability
            // that a test point drawn from P lies outside of S is bounded by some a priori specified nu between 0 and 1.
            // Source: http://scikit-learn.org/stable/modules/outlier_detection.html, http://www.cs.cmu.edu/~aarnold/ids/postal.pdf
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
