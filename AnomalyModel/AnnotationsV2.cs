using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyModel
{
    /// <summary>
    /// Anomaly-based annotation
    /// </summary>
    public class AnnotationV2
    {
        /// <summary>
        /// zero-based
        /// </summary>
        public readonly ulong Frame;

        public readonly LabelV2 Label;

        /// <summary>
        /// Horizontal location of the begin of an anomaly within the frame in pixels.
        /// The XStart coordinate belongs to the anomaly as well.
        /// </summary>
        public readonly int XStart;

        /// <summary>
        /// Horizontal location of the end of an anomaly within the frame in pixels.
        /// The XEnd coordinate belongs to the anomaly as well.
        /// </summary>
        public readonly int XEnd;

        public readonly string AnomalyId;

        public AnnotationV2(ulong frame, LabelV2 label, int xStart, int xEnd, string anomalyId = null)
        {
            Frame = frame;
            Label = label;
            XStart = xStart;
            XEnd = xEnd;
            AnomalyId = anomalyId;
        }

        public override string ToString()
        {
            return $"frame={Frame}, label={Label}, [{XStart}, {XEnd}], anomalyId={AnomalyId}";
        }
    }

    public enum LabelV2
    {
        Anomaly, Unclear
    }

    class AnnotationV2Comparer : IComparer<AnnotationV2>
    {
        public int Compare(AnnotationV2 a, AnnotationV2 b)
        {
            if (a.Frame < b.Frame)
            {
                return -1;
            }
            else if (a.Frame > b.Frame)
            {
                return 1;
            }
            else
            {
                if (a.XStart < b.XStart)
                {
                    return -1;
                }
                else if (a.XStart > b.XStart)
                {
                    return 1;
                }
                else
                {
                    if (a.XEnd < b.XEnd)
                    {
                        return -1;
                    }
                    else if (a.XEnd > b.XEnd)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }
    }


    public class AnnotationsV2
    {
        public readonly ulong[] NormalFrames;
        public readonly ulong[] AnomalyFrames;
        public readonly ulong[] UnclearFrames;

        public readonly AnnotationV2[] AnomalyRegions;
        public readonly AnnotationV2[] UnclearRegions;

        public AnnotationsV2(ulong[] normalFrames, ulong[] anomalyFrames, ulong[] unclearFrames, AnnotationV2[] anomalyRegions, AnnotationV2[] unclearRegions)
        {
            NormalFrames = normalFrames;
            AnomalyFrames = anomalyFrames;
            UnclearFrames = unclearFrames;
            AnomalyRegions = anomalyRegions;
            UnclearRegions = unclearRegions;
        }
    }

    /// <summary>
    /// Anomaly-based annotation reader (.v2.anomaly_based.ann files)
    /// </summary>
    public class AnnotationsV2Reader
    {
        private enum ReadState
        {
            None, Header, Data
        }

        /// <summary>
        /// Read annotation information from file.
        /// NormalFrames, AnomalyFrames and UnclearFrames are sorted (ascending), AnomalyRegions and UnclearRegions aren't.
        /// </summary>
        /// <param name="pathToAnnotationFile"></param>
        /// <returns></returns>
        public static AnnotationsV2 Read(string pathToAnnotationFile)
        {
            var readState = ReadState.None;
            IEnumerable<ulong> annotatedFrames = null;
            List<AnnotationV2> anomalyRegions = new List<AnnotationV2>();
            List<AnnotationV2> unclearRegions = new List<AnnotationV2>();
            foreach (var line in File.ReadLines(pathToAnnotationFile))
            {
                if (line == "HEADER")
                {
                    readState = ReadState.Header;
                }
                else if (line == "DATA")
                {
                    readState = ReadState.Data;
                }
                else if (readState == ReadState.Header && line.StartsWith("annotated_frames="))
                {
                    annotatedFrames = line.Replace("annotated_frames=", "").Split(new char[] { ',' }).Select(s => ulong.Parse(s));
                }
                else if (readState == ReadState.Data)
                {
                    var split = line.Split(new char[] { ',' });
                    Debug.Assert(split.Length == 5);

                    var frame = ulong.Parse(split[0]);
                    var label = (LabelV2)(Enum.Parse(typeof(LabelV2), split[1]));
                    var xStart = int.Parse(split[2]);
                    var xEnd = int.Parse(split[3]);
                    string anomalyId = (split[4] == "" ? null : split[4]);
                    var annotation = new AnnotationV2(frame, label, xStart, xEnd, anomalyId);
                    if (label == LabelV2.Anomaly)
                    {
                        Debug.Assert(anomalyId != null);
                        anomalyRegions.Add(annotation);
                    }
                    else if (label == LabelV2.Unclear)
                    {
                        unclearRegions.Add(annotation);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException($"Unknown label {label}");
                    }
                }
                else if (line != "")
                {
                    throw new ArgumentException($"Unexpected line {line}");
                }
            }

            var anomalyFrames = anomalyRegions.Select(ann => ann.Frame).Distinct().OrderBy(l => l).ToArray();
            var unclearFrames = unclearRegions.Select(ann => ann.Frame).Distinct().OrderBy(l => l).ToArray();
            var normalFrames = annotatedFrames
                .Except(anomalyFrames)
                .Except(unclearFrames)
                .Distinct()
                .OrderBy(l => l)
                .ToArray();

            return new AnnotationsV2(normalFrames, anomalyFrames, unclearFrames, anomalyRegions.ToArray(), unclearRegions.ToArray());
        }
    }

    public class AnnotationsV2Writer
    {
        public static void Write(string pathToAnnotationFile, ISet<ulong> annotatedFrames, List<AnnotationV2> annotations)
        {
            var builder = new StringBuilder();
            builder.AppendLine("HEADER");
            builder.AppendLine($"annotated_frames={string.Join(",", annotatedFrames.OrderBy(l => l))}");
            builder.AppendLine();
            builder.AppendLine("DATA");
            foreach (var annotation in annotations.OrderBy(a => a, new AnnotationV2Comparer()))
            {
                builder.AppendLine($"{annotation.Frame},{annotation.Label},{annotation.XStart},{annotation.XEnd},{(annotation.AnomalyId == null ? "" : annotation.AnomalyId)}");
            }

            File.WriteAllText(pathToAnnotationFile, builder.ToString());
        }
    }
}
