using ImageViewer.Domain;
using ImageViewer.Utils;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageViewer.Views
{
    /// <summary>
    /// CanvasViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CanvasViewer : UserControl
    {
        private double screenWidth;
        private double screenHeight;

        private GDALReader gdalReader;

        private MapImage mapImage;

        public CanvasViewer()
        {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent()
        {
            this.Loaded += (object sender, RoutedEventArgs e) =>
            {
                screenWidth = ActualWidth;
                screenHeight = ActualHeight;
            };

            this.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            {
                screenWidth = ActualWidth;
                screenHeight = ActualHeight;
                FitCanvas();
            };
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = "Image files (*.tif)|*.tif|All Files (*.*)|*.*";
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CanvasView.Children.Clear();

                string fileName = fileDialog.FileName;

                gdalReader = new GDALReader();
                gdalReader.Open(fileName);
                HeaderInfo headerInfo = gdalReader.GetHeaderInfo();

                ImageFormat imageFormat = ImageFormat.Bmp;
                if (fileName.ToLower().Contains(".png") || fileName.ToLower().Contains(".tif"))
                    imageFormat = ImageFormat.Png;
                else if (fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg"))
                    imageFormat = ImageFormat.Jpeg;

                mapImage = new MapImage(headerInfo, fileName);
                mapImage.ImageFormat = imageFormat;
            }
        }

        private void FitCanvas()
        {

        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void CanvasMouseLeftDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void CanvasMouseLeftUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void CanvasMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
