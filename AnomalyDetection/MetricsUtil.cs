using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyDetection
{
    public class Metrics
    {
        public readonly int TP;
        public readonly int TN;
        public readonly int FP;
        public readonly int FN;
        public readonly double Accuracy;
        public readonly double Specificity;
        public readonly double Recall;
        public readonly double Precision;
        public readonly int NFramesWithFps;
        public readonly int NFrames;
        public readonly double FpFramesPercentage;
        public readonly int TpAnomalyLevel;
        public readonly int FnAnomalyLevel;
        public readonly string MissedAnomalies;

        public Metrics(
            int tp, int tn, int fp, int fn,
            double accuracy, double specificity, double recall, double precision,
            int nFramesWithFps, int nFrames, double fpFramesPercentage,
            int tpAnomalyLevel = -1, int fnAnomalyLevel = -1, string missedAnomalies = null
        )
        {
            TP = tp;
            TN = tn;
            FP = fp;
            FN = fn;
            Accuracy = accuracy;
            Specificity = specificity;
            Recall = recall;
            Precision = precision;
            NFramesWithFps = nFramesWithFps;
            NFrames = nFrames;
            FpFramesPercentage = fpFramesPercentage;
            TpAnomalyLevel = tpAnomalyLevel;
            FnAnomalyLevel = fnAnomalyLevel;
            MissedAnomalies = missedAnomalies;
        }
    }

    public class MetricsUtil
    {
        /// <summary>
        /// Print metrics on the sample level.
        /// Label 1 = normal rope (true negative), 0 = anomaly (true positive)
        /// Normal and anomaly in the label array names is based on the frames:
        /// Normal means a frame with no anomaly and unclear regions at all --> yNormal only contains ones.
        /// Anomaly means a frame with at least one anomaly region --> yAnomaly can contain both labels.
        /// </summary>
        /// <param name="yNormalPredicted"></param>
        /// <param name="yAnomaly"></param>
        /// <param name="yAnomalyPredicted"></param>
        public static void PrintMetrics(
            int[] yNormalPredicted, int nSamplesPerFrame,
            int[] yAnomaly = null, int[] yAnomalyPredicted = null,
            Dictionary<string, ISet<int>> anomalyIdToYAnomalyIndices = null,
            ISet<int> yAnomalyUnclearIndices = null
        )
        {
            var metrics = CalculateMetrics(
                yNormalPredicted, nSamplesPerFrame,
                yAnomaly, yAnomalyPredicted, anomalyIdToYAnomalyIndices, yAnomalyUnclearIndices
            );
            PrintMetrics(metrics);
        }

        public static void PrintMetrics(Metrics metrics)
        {
            PrintPerSampleMetrics(metrics);
            if (metrics.TpAnomalyLevel != -1 && metrics.FnAnomalyLevel != -1 && metrics.MissedAnomalies != null)
            {
                PrintPerAnomalyMetrics(metrics);
            }
            Console.WriteLine();
        }

        public static Metrics CalculateMetrics(
            int[] yNormalPredicted, int nSamplesPerFrame,
            int[] yAnomaly = null, int[] yAnomalyPredicted = null,
            Dictionary<string, ISet<int>> anomalyIdToYAnomalyIndices = null,
            ISet<int> yAnomalyUnclearIndices = null
        )
        {
            Debug.Assert(yNormalPredicted.Length % nSamplesPerFrame == 0);

            bool areAnomalyAttrsSet = yAnomaly != null && yAnomalyPredicted != null && anomalyIdToYAnomalyIndices != null && yAnomalyUnclearIndices != null;

            int tp = 0;
            int tn = 0;
            int fp = 0;
            int fn = 0;

            var fpFramesNormal = new HashSet<int>();
            for (int i = 0; i < yNormalPredicted.Length; ++i)
            {
                int label = yNormalPredicted[i];
                if (label == 1)
                {
                    ++tn;
                }
                else
                {
                    ++fp;
                    fpFramesNormal.Add(i / nSamplesPerFrame);
                }
            }

            Dictionary<string, int> tpsPerAnomaly = null;
            HashSet<int> fpFramesAnomaly = null;
            if (areAnomalyAttrsSet)
            {
                Debug.Assert(yAnomaly.Length == yAnomalyPredicted.Length);
                Debug.Assert(yAnomalyPredicted.Length % nSamplesPerFrame == 0);

                tpsPerAnomaly = new Dictionary<string, int>();
                fpFramesAnomaly = new HashSet<int>();
                foreach (var anomalyId in anomalyIdToYAnomalyIndices.Keys)
                {
                    tpsPerAnomaly[anomalyId] = 0;
                }

                int i = 0;
                foreach (var labels in Enumerable.Zip(
                    yAnomaly, yAnomalyPredicted, (truth, predicted) => new { truth, predicted }
                ))
                {
                    if (labels.truth == 1 && labels.predicted == 1)
                    {
                        ++tn;
                    }
                    else if (labels.truth == 1 && labels.predicted == 0)
                    {
                        // Don't count FPs on unclear regions within a frame containing anomalies (normal frames cannot contain unclear regions).
                        if (!yAnomalyUnclearIndices.Contains(i))
                        {
                            ++fp;
                            fpFramesAnomaly.Add(i / nSamplesPerFrame);
                        }
                        else
                        {
                            ++tn;
                        }
                    }
                    else if (labels.truth == 0 && labels.predicted == 0)
                    {
                        ++tp;
                        foreach (var anomalyId in anomalyIdToYAnomalyIndices.Keys)
                        {
                            if (anomalyIdToYAnomalyIndices[anomalyId].Contains(i))
                            {
                                ++tpsPerAnomaly[anomalyId];
                            }
                        }
                    }
                    else if (labels.truth == 0 && labels.predicted == 1)
                    {
                        ++fn;
                    }

                    ++i;
                }
            }

            double accuracy = (double)(tp + tn) / (tp + tn + fp + fn);
            double specificity = (double)tn / (tn + fp);
            double recall = (double)tp / (tp + fn);
            double precision = (double)tp / (tp + fp);

            int nFramesWithFps;
            int nFrames;

            if (areAnomalyAttrsSet)
            {
                nFramesWithFps = fpFramesNormal.Count() + fpFramesAnomaly.Count;
                nFrames = yNormalPredicted.Length / nSamplesPerFrame + yAnomalyPredicted.Length / nSamplesPerFrame;
            }
            else
            {
                nFramesWithFps = fpFramesNormal.Count();
                nFrames = yNormalPredicted.Length / nSamplesPerFrame;
            }
            double fpFramesPercentage = (double)nFramesWithFps / nFrames * 100;

            if (areAnomalyAttrsSet)
            {
                int tpAnomalyLevel = tpsPerAnomaly.Values.Where(count => count > 0).Count();
                int fnAnomalyLevel = tpsPerAnomaly.Keys.Count - tpAnomalyLevel;
                string missedAnomalies = string.Join(", ", tpsPerAnomaly.Where(kv => kv.Value == 0).Select(kv => kv.Key).OrderBy(k => k));

                return new Metrics(
                    tp, tn, fp, fn,
                    accuracy, specificity, recall, precision,
                    nFramesWithFps, nFrames, fpFramesPercentage,
                    tpAnomalyLevel, fnAnomalyLevel, missedAnomalies
                );
            }
            else
            {
                return new Metrics(
                    tp, tn, fp, fn,
                    accuracy, specificity, recall, precision,
                    nFramesWithFps, nFrames, fpFramesPercentage
                );
            }
        }

        private static void PrintPerSampleMetrics(Metrics metrics)
        {
            Console.WriteLine("Per-sample metrics:");
            Console.WriteLine($"TP={metrics.TP}, TN={metrics.TN}, FP={metrics.FP}, FN={metrics.FN}");
            Console.WriteLine($"Frame percentage containing false positives: {metrics.FpFramesPercentage} ({metrics.NFramesWithFps} / {metrics.NFrames})");
            Console.WriteLine($"specificity={metrics.Specificity}, recall={metrics.Recall} (precision={metrics.Precision})");
        }

        private static void PrintPerAnomalyMetrics(Metrics metrics)
        {
            Console.WriteLine("Per-anomaly metrics:");
            Console.WriteLine($"TP={metrics.TpAnomalyLevel}, FN={metrics.FnAnomalyLevel}, FP (on sample level)={metrics.FP}");
            Console.WriteLine($"Missed anomalies: {metrics.MissedAnomalies}");
        }
    }
}
