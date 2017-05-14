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
        public static void PrintPerSampleMetrics(int[] yNormalPredicted, int[] yAnomaly = null, int[] yAnomalyPredicted = null)
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

            if (yAnomaly != null && yAnomalyPredicted != null)
            {
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
                    }
                    else if (labels.truth == 0 && labels.predicted == 1)
                    {
                        ++fn;
                    }
                }
            }

            PrintMetrics(tp, tn, fp, fn);
        }

        public static void PrintPerAnomalyMetrics()
        {

        }

        private static void PrintMetrics(int tp, int tn, int fp, int fn)
        {
            //double accuracy = (double)(tp + tn) / (tp + tn + fp + fn);
            double specificity = (double)tn / (tn + fp);
            double recall = (double)tp / (tp + fn);
            double precision = (double)tp / (tp + fp);
            Console.WriteLine($"TP={tp}, TN={tn}, FP={fp}, FN={fn}");
            Console.WriteLine($"specificity={specificity}, recall={recall} (precision={precision})");
        }
    }
}
