using System.IO;
using RawLibrary;
using System;
using System.Linq;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.ML;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV.Util;
using System.Diagnostics;
using System.Xml.Serialization;
using Emgu.Util;
using System.Xml.Linq;
using AnomalyModel;

namespace AnomalyDetection
{
    public class Program
    {
        private const bool DoSave = true;
        private const bool DoLoad = false;

        private const string PathToVideo = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.raw3";
        private const string PathToAnnotation = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.v2.anomaly_based.ann";

        private const string NSamplesPerFrameFile = "nSamplesPerFrame.txt";
        private const string CellWidthFile = "cellWidth.txt";
        private const string XTrainFile = "XTrain.xml";
        private const string XTestNormalFile = "XTestNormal.xml";
        private const string XTestAnomalyFile = "XTestAnomaly.xml";

        public static void Main(string[] args)
        {
            var annotations = AnnotationsV2Reader.Read(PathToAnnotation);

            // Shuffle normal frames before train-test-split to make test set more diverse. Use seed for reproducibility.
            // Otherwise, shuffling seems to have no effect on SVM training so this is the only place we have to do it:
            // http://stackoverflow.com/questions/20731214/is-it-required-to-shuffle-the-training-data-for-svm-multi-classification
            var random = new Random(42);
            var normalFramesTest = annotations.NormalFrames
                .OrderBy(l => random.NextDouble())
                .Take(annotations.AnomalyFrames.Length)
                .ToArray();
            var normalFramesTrain = annotations.NormalFrames.Except(normalFramesTest).ToArray();

            int nSamplesPerFrame;
            int cellWidth;
            Mat XTrain;
            Mat XTestNormal;
            Mat XTestAnomaly;
            if (DoLoad)
            {
                Console.WriteLine($"[{DateTime.Now}] Program.Main: reading matrices from filesystem");

                nSamplesPerFrame = int.Parse(File.ReadAllText(NSamplesPerFrameFile));
                cellWidth = int.Parse(File.ReadAllText(CellWidthFile));
                XTrain = new Mat();
                new FileStorage(XTrainFile, FileStorage.Mode.Read).GetFirstTopLevelNode().ReadMat(XTrain);
                XTestNormal = new Mat();
                new FileStorage(XTestNormalFile, FileStorage.Mode.Read).GetFirstTopLevelNode().ReadMat(XTestNormal);
                XTestAnomaly = new Mat();
                new FileStorage(XTestAnomalyFile, FileStorage.Mode.Read).GetFirstTopLevelNode().ReadMat(XTestAnomaly);
            }
            else
            {
                var raw = new RawImage(new FileInfo(PathToVideo));
                var featureTransformer = new HogTransformer();
                XTrain = featureTransformer.FitTransform(raw, normalFramesTrain);
                Debug.Assert(XTrain.Rows % normalFramesTrain.Length == 0);
                nSamplesPerFrame = XTrain.Rows / normalFramesTrain.Length;
                cellWidth = featureTransformer.CellWidth;

                XTestNormal = featureTransformer.Transform(raw, normalFramesTest);
                XTestAnomaly = featureTransformer.Transform(raw, annotations.AnomalyFrames);

                if (DoSave)
                {
                    Console.WriteLine($"[{DateTime.Now}] Program.Main: writing matrices to filesystem");

                    File.WriteAllText(NSamplesPerFrameFile, nSamplesPerFrame.ToString());
                    File.WriteAllText(CellWidthFile, cellWidth.ToString());
                    new FileStorage(XTrainFile, FileStorage.Mode.Write).Write(XTrain);
                    new FileStorage(XTestNormalFile, FileStorage.Mode.Write).Write(XTestNormal);
                    new FileStorage(XTestAnomalyFile, FileStorage.Mode.Write).Write(XTestAnomaly);
                }
            }
            var yTestAnomaly = new int[XTestAnomaly.Rows];
            for (int i = 0; i < yTestAnomaly.Length; ++i)
            {
                yTestAnomaly[i] = 1;
            }
            foreach (var anomalyRegion in annotations.AnomalyRegions)
            {
                int offset = Array.IndexOf(annotations.AnomalyFrames, anomalyRegion.Frame) * nSamplesPerFrame;
                // Math.Min is necessary because the last pixel columns that don't make up a whole cell are truncated by the feature transformer.
                for (int i = anomalyRegion.XStart / cellWidth; i < Math.Min(nSamplesPerFrame, anomalyRegion.XEnd / cellWidth + 1); ++i)
                {
                    yTestAnomaly[offset + i] = 0;
                }
            }

            //foreach (double gamma in new double[] { 0.8, 2.0 })
            //{
            //    foreach (double nu in new double[] { 0.004, 0.003, 0.0015 })
            //    {
            //        Console.WriteLine($"gamma={gamma}, nu={nu}");
            //        var classifier = new SvmOneClassClassifier();
            //        classifier.Fit(XTrain, gamma, nu);
            //        Predict(classifier, XTestNormal, XTestAnomaly, nSamplesPerFrame);
            //        Console.WriteLine();
            //    }
            //}

            var classifier = new SvmOneClassClassifier();

            // perfect specificity (per-frame grid search)
            classifier.Fit(XTrain, 0.8, 0.004);

            // better recall, specificity too low though (per-frame grid search)
            // classifier.Fit(XTrain, 2.0, 0.003);

            // TODO anomaly metric
            // TODO grid search

            Console.WriteLine();
            Console.WriteLine("TRAIN");
            var yTrainPredicted = classifier.Predict(XTrain);
            Metrics.PrintPerSampleMetrics(yTrainPredicted);
            Console.WriteLine();

            Console.WriteLine("TEST");
            var yTestNormalPredicted = classifier.Predict(XTestNormal);
            var yTestAnomalyPredicted = classifier.Predict(XTestAnomaly);
            Metrics.PrintPerSampleMetrics(yTestNormalPredicted, yTestAnomaly, yTestAnomalyPredicted);
        }
    }
}
