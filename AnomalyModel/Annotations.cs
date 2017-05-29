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
    public class Annotation
    {
        /// <summary>
        /// zero-based
        /// </summary>
        public readonly ulong Frame;

        public readonly AnnotationLabel Label;

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

        public Annotation(ulong frame, AnnotationLabel label, int xStart, int xEnd, string anomalyId = null)
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

    public enum AnnotationLabel
    {
        Anomaly, Unclear
    }

    class AnnotationComparer : IComparer<Annotation>
    {
        public int Compare(Annotation a, Annotation b)
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


    public class Annotations
    {
        public readonly ulong[] NormalFrames;
        public readonly ulong[] AnomalyFrames;
        public readonly ulong[] UnclearFrames;

        public readonly Annotation[] AnomalyRegions;
        public readonly Annotation[] UnclearRegions;

        public Annotations(ulong[] normalFrames, ulong[] anomalyFrames, ulong[] unclearFrames, Annotation[] anomalyRegions, Annotation[] unclearRegions)
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
    public class AnnotationsReader
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
        public static Annotations Read(string pathToAnnotationFile)
        {
            var readState = ReadState.None;
            IEnumerable<ulong> annotatedFrames = null;
            List<Annotation> anomalyRegions = new List<Annotation>();
            List<Annotation> unclearRegions = new List<Annotation>();
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
                    var label = (AnnotationLabel)(Enum.Parse(typeof(AnnotationLabel), split[1]));
                    var xStart = int.Parse(split[2]);
                    var xEnd = int.Parse(split[3]);
                    string anomalyId = (split[4] == "" ? null : split[4]);
                    var annotation = new Annotation(frame, label, xStart, xEnd, anomalyId);
                    if (label == AnnotationLabel.Anomaly)
                    {
                        Debug.Assert(anomalyId != null);
                        anomalyRegions.Add(annotation);
                    }
                    else if (label == AnnotationLabel.Unclear)
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

            return new Annotations(normalFrames, anomalyFrames, unclearFrames, anomalyRegions.ToArray(), unclearRegions.ToArray());
        }
    }

    public class AnnotationsWriter
    {
        public static void Write(string pathToAnnotationFile, ISet<ulong> annotatedFrames, List<Annotation> annotations)
        {
            var builder = new StringBuilder();
            builder.AppendLine("HEADER");
            builder.AppendLine($"annotated_frames={string.Join(",", annotatedFrames.OrderBy(l => l))}");
            builder.AppendLine();
            builder.AppendLine("DATA");
            foreach (var annotation in annotations.OrderBy(a => a, new AnnotationComparer()))
            {
                builder.AppendLine($"{annotation.Frame},{annotation.Label},{annotation.XStart},{annotation.XEnd},{(annotation.AnomalyId == null ? "" : annotation.AnomalyId)}");
            }

            File.WriteAllText(pathToAnnotationFile, builder.ToString());
        }
    }
}
