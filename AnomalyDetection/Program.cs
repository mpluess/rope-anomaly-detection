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
        private const bool DoSave = false;
        private const bool DoLoad = false;

        private const string PathToVideo = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.raw3";
        private const string PathToAnnotation = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.v1.frame_based.ann";

        private const string NSamplesPerFrameFile = "nSamplesPerFrame.txt";
        private const string XTrainFile = "XTrain.xml";
        private const string XTestNormalFile = "XTestNormal.xml";
        private const string XTestAnomalyFile = "XTestAnomaly.xml";

        public static void Main(string[] args)
        {
            var annotation = AnnotationsV1Reader.ReadAnnotations(PathToAnnotation);

            // Shuffling seems to have no effect on SVM training so we can skip it:
            // http://stackoverflow.com/questions/20731214/is-it-required-to-shuffle-the-training-data-for-svm-multi-classification
            var anomalyFrames = annotation.AnomalyFrames;
            var normalFramesTest = annotation.NormalFrames.Take(anomalyFrames.Length).ToArray();
            var normalFramesTrain = annotation.NormalFrames.Except(normalFramesTest).ToArray();

            int nSamplesPerFrame;
            Mat XTrain;
            Mat XTestNormal;
            Mat XTestAnomaly;
            if (DoLoad)
            {
                Console.WriteLine($"[{DateTime.Now}] Program.Main: reading matrices from filesystem");

                nSamplesPerFrame = int.Parse(File.ReadAllText(NSamplesPerFrameFile));
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
                // TODO normalize
                XTrain = featureTransformer.FitTransform(raw, normalFramesTrain);
                Debug.Assert(XTrain.Rows % normalFramesTrain.Length == 0);
                nSamplesPerFrame = XTrain.Rows / normalFramesTrain.Length;

                XTestNormal = featureTransformer.Transform(raw, normalFramesTest);
                XTestAnomaly = featureTransformer.Transform(raw, anomalyFrames);

                if (DoSave)
                {
                    Console.WriteLine($"[{DateTime.Now}] Program.Main: writing matrices to filesystem");

                    File.WriteAllText(NSamplesPerFrameFile, nSamplesPerFrame.ToString());
                    new FileStorage(XTrainFile, FileStorage.Mode.Write).Write(XTrain);
                    new FileStorage(XTestNormalFile, FileStorage.Mode.Write).Write(XTestNormal);
                    new FileStorage(XTestAnomalyFile, FileStorage.Mode.Write).Write(XTestAnomaly);
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

            // perfect specificity
            classifier.Fit(XTrain, 0.8, 0.004);

            // better recall
            // classifier.Fit(XTrain, 2.0, 0.003);

            Predict(classifier, XTestNormal, normalFramesTest, XTestAnomaly, anomalyFrames, nSamplesPerFrame, verbose: true);
            Console.WriteLine();
        }

        private static void Predict(
            SvmOneClassClassifier classifier,
            Mat XTestNormal, ulong[] normalFrames, Mat XTestAnomaly, ulong[] anomalyFrames,
            int nSamplesPerFrame, bool verbose = false)
        {
            int tp = 0;
            int tn = 0;
            int fp = 0;
            int fn = 0;
            // TODO print matching frameNr
            Console.WriteLine("Normal frames:");
            foreach (bool isAnomaly in classifier.Predict(XTestNormal, normalFrames, nSamplesPerFrame, verbose))
            {
                if (isAnomaly)
                {
                    fp += 1;
                }
                else
                {
                    tn += 1;
                }
            }

            Console.WriteLine("Anomaly frames:");
            foreach (bool isAnomaly in classifier.Predict(XTestAnomaly, anomalyFrames, nSamplesPerFrame, verbose))
            {
                if (isAnomaly)
                {
                    tp += 1;
                }
                else
                {
                    fn += 1;
                }
            }

            //double accuracy = (double)(tp + tn) / (tp + tn + fp + fn);
            double specificity = (double)tn / (tn + fp);
            double recall = (double)tp / (tp + fn);
            double precision = (double)tp / (tp + fp);
            Console.WriteLine($"TP={tp}, TN={tn}, FP={fp}, FN={fn}");
            Console.WriteLine($"specificity={specificity}, recall={recall} (precision={precision})");
        }
    }
}
