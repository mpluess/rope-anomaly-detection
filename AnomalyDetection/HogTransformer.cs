using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using RawLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomalyDetection
{
    public enum CellType { Background, Edge, Rope };

    [Serializable]
    public struct RopeLocation
    {
        /// <summary>
        /// Index of top cell, start included.
        /// </summary>
        public readonly int StartCellY;
        /// <summary>
        /// Index of bottom cell, end included.
        /// </summary>
        public readonly int EndCellY;

        /// <summary>
        /// Width of the rope in cells.
        /// Corresponds to image height since the rope runs horizontally.
        /// </summary>
        public readonly int RopeWidthCells;

        /// <summary>
        /// Percentage of background pixels per cell from top to bottom, in the range [0, 1].
        /// </summary>
        public readonly float[] BackgroundPixelPercentages;

        public RopeLocation(int startCellY, int endCellY, float[] backgroundPixelPercentages)
        {
            Debug.Assert(endCellY >= startCellY);
            StartCellY = startCellY;
            EndCellY = endCellY;
            RopeWidthCells = EndCellY - StartCellY + 1;
            BackgroundPixelPercentages = backgroundPixelPercentages;
        }
    }

    /// <summary>
    /// Class to transform frames to a feature matrix X with dimensions (n_samples, n_features).
    /// The samples are parts of a frame CellWidth pixels wide consisting of RopeWidthCells cells.
    /// The features are NBins HOG features, the entropy of the HOG features
    /// and the percentage of pixels <= MaxBackgroundIntensity --> 6 per cell --> n_features = 6 * RopeWidthCells.
    /// 
    /// The API is based on sklearn transformers (http://scikit-learn.org).
    /// "..., these are represented by classes with a fit method, which learns model parameters (e.g. mean and standard deviation for normalization)
    /// from a training set, and a transform method which applies this transformation model to unseen data.
    /// fit_transform may be more convenient and efficient for modelling and transforming the training data simultaneously."
    /// 
    /// Important: expects a video where the rope runs horizontally!
    /// </summary>
    public class HogTransformer
    {
        public readonly int CellWidth;
        public readonly int CellHeight;
        public readonly double MaxBackgroundIntensity;
        public readonly double EdgeThreshold;
        public readonly double BackgroundThreshold;
        public readonly int NBins;

        /// <summary>
        /// Width of the segmented rope.
        /// Since the rope runs horizontally this actually maps to the HEIGHT in the image!
        /// </summary>
        private int RopeWidthCells;

        public HogTransformer(
            int cellWidth = 20, int cellHeight = 20, double maxBackgroundIntensity = 35.0,
            double edgeThreshold = 0.1, double backgroundThreshold = 0.96, int nBins = 4)
        {
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            MaxBackgroundIntensity = maxBackgroundIntensity;
            EdgeThreshold = edgeThreshold;
            BackgroundThreshold = backgroundThreshold;
            NBins = nBins;
        }

        /// <summary>
        /// Segment the image, determine and set the number of cells constituting a sample by estimating the rope width.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="normalFrames"></param>
        /// <param name="ropeLocations"></param>
        /// <returns>Rope locations per frame and horizontal cell (= sample).</returns>
        public RopeLocation[][] Fit(RawImage raw, ulong[] normalFrames, RopeLocation[][] ropeLocations = null)
        {
            if (ropeLocations == null)
            {
                Console.WriteLine($"[{DateTime.Now}] HogTransformer.Fit: segmenting rope");
                ropeLocations = SegmentRopeForFrames(raw, normalFrames);
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] HogTransformer.Transform: rope already segmented");
            }

            // Describes the width of the rope but since the rope runs horizontally in the image, this is actually the y-coordinate of the image.
            var ropeWidthsCells = ropeLocations.SelectMany(array => array).Select(location => location.RopeWidthCells);
            int minRopeWidthCells = ropeWidthsCells.Min();
            int maxRopeWidthCells = ropeWidthsCells.Max();

            Console.WriteLine($"Rope width global min: {minRopeWidthCells}");
            //Console.WriteLine($"frames with min width: {string.Join(", ", Enumerable.Zip(normalFrames, ropeLocations, (nr, array) => Tuple.Create(nr, array)).Where(t => t.Item2.Select(location => location.RopeWidthCells).Contains(minRopeWidthCells)).Select(t => $"{t.Item1} ({Array.IndexOf(t.Item2.Select(l => l.RopeWidthCells).ToArray(), minRopeWidthCells)})"))}");
            Console.WriteLine($"Rope width global max: {maxRopeWidthCells}");
            //Console.WriteLine($"frames with max width: {string.Join(", ", Enumerable.Zip(normalFrames, ropeLocations, (nr, array) => Tuple.Create(nr, array)).Where(t => t.Item2.Select(location => location.RopeWidthCells).Contains(maxRopeWidthCells)).Select(t => $"{t.Item1} ({Array.IndexOf(t.Item2.Select(l => l.RopeWidthCells).ToArray(), maxRopeWidthCells)})"))}");

            RopeWidthCells = maxRopeWidthCells;

            return ropeLocations;
        }

        /// <summary>
        /// Segment the image, determine and set the number of cells constituting a sample and transform frames to a HOG feature matrix.
        /// 
        /// Use this method for the train set.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="normalFrames"></param>
        /// <returns>HOG feature matrix with dimensions (|normalFrames| * FrameWidth / CellWidth) x ((NBins + 2) * RopeWidthCells)</returns>
        public Mat FitTransform(RawImage raw, ulong[] normalFrames)
        {
            Console.WriteLine($"[{DateTime.Now}] HogTransformer.FitTransform: calling Fit");
            var ropeLocations = Fit(raw, normalFrames);


            Console.WriteLine($"[{DateTime.Now}] HogTransformer.FitTransform: calling Transform");
            return Transform(raw, normalFrames, ropeLocations);
        }

        /// <summary>
        /// Segment the image and transform frames to a HOG feature matrix.
        /// One row of the matrix = one sample describes a part of the rope which is CellWidth pixels high
        /// and RopeWidthCells * CellHeight pixels wide (= the whole rope diameter).
        /// 
        /// Use this method for the test set.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="frames"></param>
        /// <param name="ropeLocations"></param>
        /// <returns>HOG feature matrix with dimensions (|frames| * FrameWidth / CellWidth) x ((NBins + 2) * RopeWidthCells)</returns>
        public Mat Transform(RawImage raw, ulong[] frames, RopeLocation[][] ropeLocations = null)
        {
            if (ropeLocations == null)
            {
                Console.WriteLine($"[{DateTime.Now}] HogTransformer.Transform: segmenting rope");
                ropeLocations = SegmentRopeForFrames(raw, frames);
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] HogTransformer.Transform: rope already segmented");
            }

            Console.WriteLine($"[{DateTime.Now}] HogTransformer.Transform: transforming images to HOG features");
            VectorOfMat samples = new VectorOfMat();
            foreach (var frameWithRopeLocations in Enumerable.Zip(frames, ropeLocations, (frameNr, locations) => new { frameNr = frameNr, ropeLocations = locations }))
            {
                raw.ReadFrame(frameWithRopeLocations.frameNr);
                using (Mat ropeFrame = MatUtil.RawToMat(raw))
                {
                    AddFeatureVectorsOfFrame(samples, ropeFrame, frameWithRopeLocations.ropeLocations);
                }
            }

            // Transform the features for SVM
            Console.WriteLine($"[{DateTime.Now}] HogTransformer.Transform: calling ConvertToMl");
            return MatUtil.ConvertToMl(samples);
        }

        private RopeLocation[][] SegmentRopeForFrames(RawImage raw, ulong[] frames)
        {
            var ropeLocations = new RopeLocation[frames.Length][];
            int rlIndex = 0;
            foreach (ulong frameNr in frames)
            {
                raw.ReadFrame(frameNr);
                using (Mat ropeFrame = MatUtil.RawToMat(raw))
                {
                    ropeLocations[rlIndex] = SegmentRopeForFrame(ropeFrame, frameNr);
                    ++rlIndex;
                }
            }

            return ropeLocations;
        }

        private RopeLocation[] SegmentRopeForFrame(Mat imageMatrix, ulong frameNr)
        {
            var image = imageMatrix.ToImage<Gray, byte>();
            int imageHeightCells = image.Rows / CellHeight;
            int imageWidthCells = image.Cols / CellWidth;
            var cellTypes = new CellType[imageHeightCells][];
            var backgroundPixelPercentages = new float[imageHeightCells][];

            // Integer division to ignore the last rows.
            for (int cellY = 0; cellY < imageHeightCells; ++cellY)
            {
                cellTypes[cellY] = new CellType[imageWidthCells];
                backgroundPixelPercentages[cellY] = new float[imageWidthCells];

                // Integer division to ignore the last columns.
                for (int cellX = 0; cellX < imageWidthCells; ++cellX)
                {
                    int nBackgroundPixels = 0;
                    for (int y = cellY * CellHeight; y < (cellY + 1) * CellHeight; ++y)
                    {
                        for (int x = cellX * CellWidth; x < (cellX + 1) * CellWidth; ++x)
                        {
                            if (image[y, x].Intensity <= MaxBackgroundIntensity)
                            {
                                ++nBackgroundPixels;
                            }
                        }
                    }

                    double backgroundPixelPercentage = (double)nBackgroundPixels / (CellWidth * CellHeight);
                    backgroundPixelPercentages[cellY][cellX] = (float)backgroundPixelPercentage;
                    if (backgroundPixelPercentage < EdgeThreshold)
                    {
                        cellTypes[cellY][cellX] = CellType.Rope;
                    }
                    else if (backgroundPixelPercentage < BackgroundThreshold)
                    {
                        cellTypes[cellY][cellX] = CellType.Edge;
                    }
                    else
                    {
                        cellTypes[cellY][cellX] = CellType.Background;
                    }
                }
            }

            var ropeLocations = new RopeLocation[imageWidthCells];
            for (int cellX = 0; cellX < imageWidthCells; ++cellX)
            {
                CellType? lastCellType = null;
                int startY = -1;
                int endY = -1;
                for (int cellY = 0; cellY < imageHeightCells; ++cellY)
                {
                    var currentCellType = cellTypes[cellY][cellX];
                    if (lastCellType.HasValue)
                    {
                        if (startY == -1 && lastCellType.Value == CellType.Background && currentCellType == CellType.Edge && cellY < imageHeightCells - 1)
                        {
                            startY = cellY + 1;
                        }
                        else if (startY == -1 && lastCellType.Value == CellType.Background && currentCellType == CellType.Rope)
                        {
                            startY = cellY;
                        }
                        else if (startY != -1 && lastCellType.Value == CellType.Edge && currentCellType == CellType.Background && cellY - 2 >= startY)
                        {
                            endY = cellY - 2;
                        }
                        else if (startY != -1 && lastCellType.Value == CellType.Rope && currentCellType == CellType.Background)
                        {
                            endY = cellY - 1;
                        }
                        else if (startY != -1 && endY == -1 && currentCellType == CellType.Background)
                        {
                            throw new InvalidOperationException($"Found background cell after start / before end, please check manually. cellX={cellX}, frameNr={frameNr}, startY={startY}, endY={endY}");
                        }
                    }
                    lastCellType = currentCellType;
                }

                if (startY != -1 && endY != -1)
                {
                    // Include upper edge
                    --startY;

                    // Include lower edge
                    ++endY;

                    // Get all percentages of this sample
                    var sampleBackgroundPixelPercentages = backgroundPixelPercentages.Select(array => array[cellX]).ToArray();
                    ropeLocations[cellX] = new RopeLocation(startY, endY, sampleBackgroundPixelPercentages);
                }
                else
                {
                    throw new InvalidOperationException($"Could not find startY and / or endY. cellX={cellX}, frameNr={frameNr}, startY={startY}, endY={endY}");
                }
            }

            return ropeLocations;
        }

        /// <summary>
        /// Create feature vectors for all samples in one frame (imageMatrix) and add them to samples.
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="imageMatrix"></param>
        /// <param name="ropeLocations"></param>
        private void AddFeatureVectorsOfFrame(VectorOfMat samples, Mat imageMatrix, RopeLocation[] ropeLocations)
        {
            Size size = imageMatrix.Size;
            Size cellSize = new Size(CellWidth, CellHeight);
            Size blockSize = new Size(cellSize.Width, RopeWidthCells * cellSize.Height);
            Size windowSize = blockSize;
            Size blockStride = cellSize;
            Size windowStride = blockSize;

            // Image width: cut off last pixels not making up a whole cell anymore using integer division
            for (int cellX = 0; cellX < size.Width / cellSize.Width; ++cellX)
            {
                var location = ropeLocations[cellX];
                
                int y = location.StartCellY * cellSize.Height;
                int height = RopeWidthCells * cellSize.Height;
                Debug.Assert(y + height < size.Height, "Image is too small to add additional cells.");
                using (
                    Mat ropeOnlyImage = new Mat(
                        imageMatrix,
                        new Rectangle(
                            cellX * cellSize.Width, y,
                            cellSize.Width, height
                        )
                    )
                )
                {
                    var hog = new HOGDescriptor(windowSize, blockSize, blockStride, cellSize, nbins: NBins);
                    float[] hogResult = hog.Compute(ropeOnlyImage, winStride: windowStride);
                    Debug.Assert(hogResult.Length == NBins * RopeWidthCells);

                    // Add entropy and background pixel percentage features per cell.
                    int nFeaturesPerCell = NBins + 2;
                    var sample = new float[nFeaturesPerCell * RopeWidthCells];
                    for (int cellNr = 0; cellNr < RopeWidthCells; ++cellNr)
                    {
                        var cell = new float[NBins];
                        for (int binNr = 0; binNr < NBins; ++binNr)
                        {
                            sample[cellNr * nFeaturesPerCell + binNr] = hogResult[cellNr * NBins + binNr];
                            cell[binNr] = hogResult[cellNr * NBins + binNr];
                        }

                        // Return value per cell: vector gradient magnitudes, normalized with L2-hys per block, gamma-corrected.
                        // --> vector sum != 1
                        // For the entropy calculation we need probabilities per bin so we have to normalize the cell vector first to have sum = 1.
                        var probs = ToProbs(cell);
                        float entropy = CalculateEntropy(probs);
                        sample[cellNr * nFeaturesPerCell + NBins] = entropy;

                        sample[cellNr * nFeaturesPerCell + NBins + 1] = location.BackgroundPixelPercentages[location.StartCellY + cellNr];
                    }

                    Mat feature = new Mat(new Size(sample.Length, 1), DepthType.Cv32F, 1);
                    feature.SetTo(sample);
                    samples.Push(feature);
                }
            }
        }

        /// <summary>
        /// Normalize vector so its sum is equal to 1.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private float[] ToProbs(float[] vector)
        {
            foreach (float f in vector)
            {
                Debug.Assert(f >= 0.0f);
            }

            float sum = vector.Sum();
            return vector.Select(f => f / sum).ToArray();
        }

        private float CalculateEntropy(float[] probs)
        {
            return -probs.Select(p => (float)(p * Math.Log(p, 2.0))).Sum();
        }
    }
}
