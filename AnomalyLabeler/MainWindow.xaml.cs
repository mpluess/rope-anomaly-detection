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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PathToVideo = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.raw3";
        private const string PathToAnnotation = @"D:\Users\Michel\Documents\FH\module\8_woipv\input\videos\Seil_2_2016-05-23_RAW3\2016-05-23_15-02-14.v2.anomaly_based.ann";

        private const ulong StartFrame = 0;

        private RawImage raw;

        private bool hasLabelingStarted;
        private int xStart;

        private SortedSet<ulong> annotatedFrames;
        private SortedSet<AnnotationV2> annotations;

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

            CurrentFrameTextBlock.Text = GetCurrentFrame().ToString();

            RopeImageControl.Source = raw.Source;
            RopeImageControl.Width = raw.ImageWidth;
            RopeImageControl.Height = raw.ImageHeight;

            hasLabelingStarted = false;
            annotatedFrames = new SortedSet<ulong>();
            annotations = new SortedSet<AnnotationV2>(new AnnotationV2Comparer());
        }

        #region callbacks
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            annotatedFrames.Add(GetCurrentFrame());
            WriteAnnotationsFile();

            raw.ReadNextFrame();
            CurrentFrameTextBlock.Text = GetCurrentFrame().ToString();
        }

        private void RopeImageControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(RopeImageControl);
            if (hasLabelingStarted)
            {
                int xEnd = (int)(p.X);
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

                hasLabelingStarted = false;
            }
            else
            {
                xStart = (int)(p.X);
                hasLabelingStarted = true;
            }
        }
        #endregion

        #region methods
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

        private void WriteAnnotationsFile()
        {
            var builder = new StringBuilder();
            builder.AppendLine("HEADER");
            builder.AppendLine($"annotated_frames={string.Join(",", annotatedFrames)}");
            builder.AppendLine();
            builder.AppendLine("DATA");
            foreach (var annotation in annotations)
            {
                builder.AppendLine($"{annotation.Frame},{annotation.Label},{annotation.XStart},{annotation.XEnd}");
            }

            File.WriteAllText(PathToAnnotation, builder.ToString());
        }
        #endregion
    }
}
