using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyModel
{
    /// <summary>
    /// Frame-based annotation
    /// </summary>
    public class AnnotationsV1
    {
        public readonly ulong[] NormalFrames;
        public readonly ulong[] AnomalyFrames;

        public AnnotationsV1(ulong[] normalFrames, ulong[] anomalyFrames)
        {
            NormalFrames = normalFrames;
            AnomalyFrames = anomalyFrames;
        }
    }

    /// <summary>
    /// Frame-based annotation reader (.v1.frame_based.ann files)
    /// </summary>
    public class AnnotationsV1Reader
    {
        public static AnnotationsV1 ReadAnnotations(string pathToAnnotationFile)
        {
            var allFrames = new SortedSet<ulong>();
            var anomalyFrames = new SortedSet<ulong>();
            foreach (var line in File.ReadLines(pathToAnnotationFile))
            {
                if (!(line.StartsWith("#")) && !(line == ""))
                {
                    if (line.StartsWith("ANNOTATED_RANGE="))
                    {
                        var split = line.Replace("ANNOTATED_RANGE=", "").Split(new char[] { '-' });
                        AddRangeToSet(split, allFrames);
                    }
                    else
                    {
                        var split = line.Split(new char[] { '-' });
                        AddRangeToSet(split, anomalyFrames);
                    }
                }
            }
            var normalFrames = new SortedSet<ulong>(allFrames.Except(anomalyFrames));

            return new AnnotationsV1(normalFrames.ToArray(), anomalyFrames.ToArray());
        }

        private static void AddRangeToSet(string[] range, ISet<ulong> set)
        {
            if (range.Length == 1)
            {
                set.Add(ulong.Parse(range[0]));
            }
            else if (range.Length == 2)
            {
                for (ulong i = ulong.Parse(range[0]); i <= ulong.Parse(range[1]); ++i)
                {
                    set.Add(i);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid line: {string.Join("", range)}");
            }
        }
    }
}
