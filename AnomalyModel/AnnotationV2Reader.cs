using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyModel
{
    /// <summary>
    /// Anomaly-based annotation
    /// </summary>
    public struct AnnotationV2
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

        public AnnotationV2(ulong frame, LabelV2 label, int xStart, int xEnd)
        {
            Frame = frame;
            Label = label;
            XStart = xStart;
            XEnd = xEnd;
        }

        public override string ToString()
        {
            return $"frame={Frame}, label={Label}, [{XStart}, {XEnd}]";
        }
    }

    public enum LabelV2
    {
        Anomaly, Unclear
    }

    public class AnnotationsV2
    {

    }

    /// <summary>
    /// Anomaly-based annotation reader (.v2.anomaly_based.ann files)
    /// </summary>
    public class AnnotationsV2Reader
    {

    }
}
