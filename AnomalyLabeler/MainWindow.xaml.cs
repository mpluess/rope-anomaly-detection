using AnomalyModel;
using RawLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnomalyLabeler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Config
        private const string PathToVideo = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.raw3";
        // private const string PathToAnnotation = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.v2.anomaly_based.ann";
        private const string PathToAnnotation = @"test.v2.anomaly_based.ann";

        private const ulong StartFrame = 0;

        /// <summary>
        /// If the user clicks to a position closer than or equal to this number to the left or right border of the image,
        /// the position will be automatically adjusted to match the border exactly.
        /// </summary>
        private const int BorderSnapThreshold = 5;


        // Attributes
        private RawImage raw;

        private bool hasLabelingStarted;
        private int xStart;

        private ISet<ulong> annotatedFrames;
        private List<AnnotationV2> annotations;

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists(PathToAnnotation))
            {
                MessageBox.Show(
                    $"File {PathToAnnotation} exists, please choose another file. Modifying an existing annotation file is not supported.",
                    "Error: file exists",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }

            raw = new RawImage(new FileInfo(PathToVideo));
            raw.ReadFrame(StartFrame);

            CurrentFrameTextBlock.Text = $"Current frame: {GetCurrentFrame()}";

            RopeImageControl.Source = raw.Source;
            RopeImageControl.Width = raw.ImageWidth;
            RopeImageControl.Height = raw.ImageHeight;
            RopeImageOverlayCanvas.Width = raw.ImageWidth;
            RopeImageOverlayCanvas.Height = raw.ImageHeight;

            hasLabelingStarted = false;
            annotatedFrames = new HashSet<ulong>();
            annotations = new List<AnnotationV2>();
        }

        #region callbacks
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            annotatedFrames.Add(GetCurrentFrame());
            AnnotationsV2Writer.Write(PathToAnnotation, annotatedFrames, annotations);

            raw.ReadNextFrame();
            CurrentFrameTextBlock.Text = "Current frame: " + GetCurrentFrame().ToString();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            annotatedFrames.Add(GetCurrentFrame());
            AnnotationsV2Writer.Write(PathToAnnotation, annotatedFrames, annotations);

            raw.ReadPreviousFrame();
            CurrentFrameTextBlock.Text = "Current frame: " + GetCurrentFrame().ToString();
        }

        private void RopeImageControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(RopeImageControl);
            if (hasLabelingStarted)
            {
                int xEnd = (int)(p.X);
                if (xEnd >= RopeImageOverlayCanvas.Width - 1 - BorderSnapThreshold)
                {
                    xEnd = (int)(RopeImageOverlayCanvas.Width - 1);
                }
                AddVerticalLineToCanvas(RopeImageOverlayCanvas, xEnd);
                if (xEnd < xStart)
                {
                    int xStartOrig = xStart;
                    xStart = xEnd;
                    xEnd = xStartOrig;
                }

                var messageBox = CreateMessageBox(xStart, xEnd);
                var result = messageBox.Show();
                if (result == Gat.Controls.MessageBoxResult.Yes)
                {
                    annotations.Add(new AnnotationV2(GetCurrentFrame(), LabelV2.Anomaly, xStart, xEnd));
                }
                else if (result == Gat.Controls.MessageBoxResult.No)
                {
                    annotations.Add(new AnnotationV2(GetCurrentFrame(), LabelV2.Unclear, xStart, xEnd));
                }

                RopeImageOverlayCanvas.Children.Clear();
                hasLabelingStarted = false;
            }
            else
            {
                xStart = (int)(p.X);
                if (xStart <= BorderSnapThreshold)
                {
                    xStart = 0;
                }
                AddVerticalLineToCanvas(RopeImageOverlayCanvas, xStart);
                hasLabelingStarted = true;
            }
        }
        #endregion

        #region methods
        private void AddVerticalLineToCanvas(Canvas canvas, int x)
        {
            var line = new Line();
            line.X1 = x;
            line.X2 = x;
            line.Y1 = 0;
            line.Y2 = canvas.Width - 1;
            line.Stroke = Brushes.White;
            line.StrokeThickness = 1;

            canvas.Children.Add(line);
        }

        private Gat.Controls.MessageBoxViewModel CreateMessageBox(int xStart, int xEnd)
        {
            var messageBox = (Gat.Controls.MessageBoxViewModel)(new Gat.Controls.MessageBoxView().FindResource("ViewModel"));
            messageBox.Message = $"Label the region from xStart={xStart} to xEnd={xEnd} as...";
            messageBox.Caption = "Labeling";
            messageBox.YesVisibility = true;
            messageBox.NoVisibility = true;
            messageBox.CancelVisibility = true;
            messageBox.OkVisibility = false;
            messageBox.Yes = "Anomaly";
            messageBox.No = "Unclear";
            messageBox.Owner = this;
            messageBox.Position = Gat.Controls.MessageBoxPosition.CenterOwner;

            return messageBox;
        }

        private ulong GetCurrentFrame()
        {
            return raw.Raw.CurrentFrame - 1;
        }
        #endregion
    }
}
