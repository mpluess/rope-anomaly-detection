using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyDetection
{
    public class Metrics
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
            int[] yNormalPredicted,
            int[] yAnomaly = null, int[] yAnomalyPredicted = null, Dictionary<string, ISet<int>> anomalyIdToYTestAnomalyIndices = null
        )
        {
            int tp = 0;
            int tn = 0;
            int fp = 0;
            int fn = 0;

            foreach (var label in yNormalPredicted)
            {
                if (label == 1)
                {
                    ++tn;
                }
                else
                {
                    ++fp;
                }
            }

            Dictionary<string, int> tpsPerAnomaly = null;
            if (yAnomaly != null && yAnomalyPredicted != null && anomalyIdToYTestAnomalyIndices != null)
            {
                tpsPerAnomaly = new Dictionary<string, int>();
                foreach (var anomalyId in anomalyIdToYTestAnomalyIndices.Keys)
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
                        ++fp;
                    }
                    else if (labels.truth == 0 && labels.predicted == 0)
                    {
                        ++tp;
                        foreach (var anomalyId in anomalyIdToYTestAnomalyIndices.Keys)
                        {
                            if (anomalyIdToYTestAnomalyIndices[anomalyId].Contains(i))
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

            PrintPerSampleMetrics(tp, tn, fp, fn);
            if (tpsPerAnomaly != null)
            {
                PrintPerAnomalyMetrics(tpsPerAnomaly, fp);
            }
        }

        private static void PrintPerSampleMetrics(int tp, int tn, int fp, int fn)
        {
            //double accuracy = (double)(tp + tn) / (tp + tn + fp + fn);
            double specificity = (double)tn / (tn + fp);
            double recall = (double)tp / (tp + fn);
            double precision = (double)tp / (tp + fp);
            Console.WriteLine("Per-sample metrics:");
            Console.WriteLine($"TP={tp}, TN={tn}, FP={fp}, FN={fn}");
            Console.WriteLine($"specificity={specificity}, recall={recall} (precision={precision})");
        }

        private static void PrintPerAnomalyMetrics(Dictionary<string, int> tpsPerAnomaly, int fpsSampleLevel)
        {
            int tp = tpsPerAnomaly.Values.Where(count => count > 0).Count();
            int fn = tpsPerAnomaly.Keys.Count - tp;
            Console.WriteLine($"TP={tp}, FN={fn}, FP (on sample level)={fpsSampleLevel}");
            Console.WriteLine($"Missed anomalies: {string.Join(", ", tpsPerAnomaly.Where(kv => kv.Value == 0).Select(kv => kv.Key).OrderBy(k => k))}");
        }
    }
}
