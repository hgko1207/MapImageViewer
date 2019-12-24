using ImageViewer.Domain;
using ImageViewer.Events;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace ImageViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EventAggregator.MouseMoveEvent.Subscribe(CanvasMouseMoveEvent);
            EventAggregator.ProgressEvent.Subscribe(ProgressEvent);
            EventAggregator.ImageOpenEvent.Subscribe(ImageOpenEvent);
        }

        private void CommonCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CanvasViewer.OpenExecuted();
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CanvasViewer.SaveExecuted();
        }

        private void ZoomFitClick(object sender, RoutedEventArgs e)
        {
            CanvasViewer.ZoomFit();
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {
            CanvasViewer.ZoomIn();
        }

        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            CanvasViewer.ZoomOut();
        }

        private void ToggleButtonClick(object sender, RoutedEventArgs e)
        {
            CanvasViewer.SelectZoomToggle((sender as ToggleButton).IsChecked.Value);
        }

        private void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CanvasViewer != null)
            {
                CanvasViewer.ComboBoxChanged(sender, e);
            }
        }

        private void CanvasMouseMoveEvent(string value)
        {
            PointText.Text = value;
        }

        private void ProgressEvent(int value)
        {
            ImageProgress.Dispatcher.Invoke(() => ImageProgress.Value = value + 1, DispatcherPriority.Background);
        }

        private void ImageOpenEvent(MapImage mapImage)
        {
            List<string> imageList = new List<string>();
            imageList.Add(mapImage.HeaderInfo.FileName);

            ImageListBox.ItemsSource = imageList;

            string image = $"Size (X,Y) : ({mapImage.ImageWidth}, {mapImage.ImageHeight})\r\n" +
                $"Band : {mapImage.HeaderInfo.Band}\r\n" +
                $"File Type : {mapImage.HeaderInfo.FileType}\r\n" +
                $"Data Type : {mapImage.HeaderInfo.DataType}\r\n" +
                $"Interleave : {mapImage.HeaderInfo.Interleave}\r\n" +
                $"Proj : {mapImage.HeaderInfo.MapInfo.Projcs}\r\n" +
                $"Unit : {mapImage.HeaderInfo.MapInfo.Unit}\r\n";

            MapImageText.Text = image;
        }
    }
}
