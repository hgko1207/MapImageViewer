using ImageViewer.Domain;
using ImageViewer.Events;
using ImageViewer.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ImageViewer.Views
{
    public partial class CanvasViewer : UserControl
    {
        private enum MapToolMode { None, Panning, SelectZoom };

        private Boundary canvasBoundary;

        private double screenWidth;
        private double screenHeight;

        private Rectangle drawRectangle;

        private Point origin;
        private Point start;

        private int zoomLevel = 0;

        private Point imageStartPoint;
        private Point imageEndPoint;
        private Point rectStartPoint;

        private GDALReader gdalReader;

        private List<MapImage> mapImages;

        private MapToolMode mapToolMode = MapToolMode.Panning;

        public CanvasViewer()
        {
            InitializeComponent();
            InitEvent();

            mapImages = new List<MapImage>();
        }

        private void Init()
        {
            zoomLevel = 0;
            imageStartPoint = new Point(0, 0);
            imageEndPoint = new Point(0, 0);
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
                //FitCanvas();
            };
        }

        public void OpenExecuted()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image files (*.tif)|*.tif|All Files (*.*)|*.*";
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == true)
            {
                //CanvasView.Children.Clear();

                gdalReader = new GDALReader();
                gdalReader.Open(fileDialog.FileName);
                MapImage mapImage = gdalReader.GetMapImageInfo();

                if (mapImages.Count == 0)
                {
                    canvasBoundary = mapImage.ImageBoundary;

                    mapImages.Add(mapImage);
                    Init();
                    FitCanvas();
                }
                else
                {
                    if (!AddImage(mapImage))
                    {
                        MessageBox.Show("좌표계가 없어 이미지를 추가 할 수 없습니다.");
                        return;
                    }
                }

                EventAggregator.ImageOpenEvent.Publish(mapImage);
            }
        }

        public void SaveExecuted()
        {
        }

        private void FitCanvas()
        {
            if (mapImages == null || mapImages.Count == 0)
                return;

            double viewPixelPerDegreeX = mapImages[0].ViewWidth / (mapImages[0].ImageBoundary.MaxX - mapImages[0].ImageBoundary.MinX);
            double viewPixelPerDegreeY = mapImages[0].ViewHeight / (mapImages[0].ImageBoundary.MaxY - mapImages[0].ImageBoundary.MinY);
            double canvasWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
            double canvasHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;

            if (screenWidth / canvasWidth < screenHeight / canvasHeight)
            {
                canvasHeight *= screenWidth / canvasWidth;
                canvasWidth = screenWidth;
            }
            else
            {
                canvasWidth *= screenHeight / canvasHeight;
                canvasHeight = screenHeight;
            }

            viewPixelPerDegreeX = canvasWidth / (canvasBoundary.MaxX - canvasBoundary.MinX);
            viewPixelPerDegreeY = canvasHeight / (canvasBoundary.MaxY - canvasBoundary.MinY);

            foreach (MapImage mapImage in mapImages)
            {
                mapImage.ViewWidth = (mapImage.ImageBoundary.MaxX - mapImage.ImageBoundary.MinX) * viewPixelPerDegreeX;
                mapImage.ViewHeight = (mapImage.ImageBoundary.MaxY - mapImage.ImageBoundary.MinY) * viewPixelPerDegreeY;
                mapImage.ImageBoundary.CalculateMargin(canvasBoundary, viewPixelPerDegreeX, viewPixelPerDegreeY);
                SetImage(mapImage);
            }

            CenterZoom();
        }

        private void SetImage(MapImage mapImage)
        {
            System.Drawing.Bitmap bitmap = gdalReader.GetBitmap(0, 0, mapImage.ImageWidth, mapImage.ImageHeight, 5);
            Image image = new Image();
            image.Source = ImageControl.BitmapToBitmapImage(bitmap, mapImage.ImageFormat);
            image.Width = mapImage.ViewWidth;
            image.Height = mapImage.ViewHeight;
            image.Stretch = Stretch.Fill;

            if (mapImage.ImageBoundary != null)
            {
                Canvas.SetLeft(image, mapImage.ImageBoundary.Left);
                Canvas.SetTop(image, mapImage.ImageBoundary.Top);
            }
            else
            {
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
            }

            CanvasView.Children.Add(image);
        }

        private void CenterZoom()
        {
            double canvasWidth = mapImages[0].ViewWidth;
            double canvasHeight = mapImages[0].ViewHeight;
            if (mapImages[0].ImageBoundary != null)
            {
                double viewPixelPerDegreeX = mapImages[0].ViewWidth / (mapImages[0].ImageBoundary.MaxX - mapImages[0].ImageBoundary.MinX);
                double viewPixelPerDegreeY = mapImages[0].ViewHeight / (mapImages[0].ImageBoundary.MaxY - mapImages[0].ImageBoundary.MinY);
                canvasWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
                canvasHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;
            }

            var tt = TranslateTransform;
            if (screenWidth > canvasWidth)
                tt.X = (screenWidth - canvasWidth) / 2;
            else
                tt.X = -(canvasWidth - screenWidth) / 2;

            if (screenHeight > canvasHeight)
                tt.Y = (screenHeight - canvasHeight) / 2;
            else
                tt.Y = -(canvasHeight - screenHeight) / 2;
        }

        private bool AddImage(MapImage mapImage)
        {
            if (mapImage.ImageBoundary == null)
                return false;

            Boundary imageBoundary = mapImage.ImageBoundary;

            if (canvasBoundary.MinX > imageBoundary.MinX)
                canvasBoundary.MinX = imageBoundary.MinX;
            if (canvasBoundary.MinY > imageBoundary.MinY)
                canvasBoundary.MinY = imageBoundary.MinY;
            if (canvasBoundary.MaxX < imageBoundary.MaxX)
                canvasBoundary.MaxX = imageBoundary.MaxX;
            if (canvasBoundary.MaxY < imageBoundary.MaxY)
                canvasBoundary.MaxY = imageBoundary.MaxY;

            double viewPixelPerDegreeX = mapImages[0].ViewWidth / (mapImages[0].ImageBoundary.MaxX - mapImages[0].ImageBoundary.MinX);
            double viewPixelPerDegreeY = mapImages[0].ViewHeight / (mapImages[0].ImageBoundary.MaxY - mapImages[0].ImageBoundary.MinY);

            mapImage.ViewWidth = (imageBoundary.MaxX - imageBoundary.MinX) * viewPixelPerDegreeX;
            mapImage.ViewHeight = (imageBoundary.MaxY - imageBoundary.MinY) * viewPixelPerDegreeY;

            mapImages.Add(mapImage);

            imageBoundary.CalculateMargin(canvasBoundary, viewPixelPerDegreeX, viewPixelPerDegreeY);
            //Canvas.SetLeft(border, imageBoundary.Left);
            //Canvas.SetTop(border, imageBoundary.Top);
            //Canvas.SetZIndex(border, 0);

            Console.WriteLine($"mapImage : {mapImage.ImageBoundary.Left}, {mapImage.ImageBoundary.Top}");

            FitCanvas();

            return true;
        }

        private void CanvasMousWheel(object sender, MouseWheelEventArgs e)
        {
            if (mapImages.Count > 0)
            {
                Point point = e.GetPosition(CanvasView);
                Zoom(e.Delta, point);
            }
        }

        public void Zoom(int value, Point point)
        {
            double rate = 0.8;
            if (value > 0) //Zoom in, 확대
                rate = 1 / rate;

            double offsetX = Math.Abs(point.X * (1 - rate));
            double offsetY = Math.Abs(point.Y * (1 - rate));

            var tt = TranslateTransform;
            if (value > 0) //확대
            {
                if (zoomLevel > 8)
                    return;

                tt.X -= offsetX;
                tt.Y -= offsetY;
                zoomLevel += 1;
            }
            else
            {
                if (zoomLevel < -4)
                    return;

                tt.X += offsetX;
                tt.Y += offsetY;
                zoomLevel -= 1;
            }

            foreach (MapImage mapImage in mapImages)
            {
                mapImage.ViewWidth *= rate;
                mapImage.ViewHeight *= rate;

                if (mapImage.ImageBoundary != null)
                {
                    mapImage.ImageBoundary.Top *= rate;
                    mapImage.ImageBoundary.Left *= rate;
                }
                SetImage(mapImage);
                //RedrawCanvas(mapImage);
            }
        }

        private void RedrawCanvas(MapImage mapImage)
        {
            double endWidth = mapImage.ViewWidth;
            double endHeight = mapImage.ViewHeight;

            double startX = 0;
            double startY = 0;

            var tt = TranslateTransform;
            if (tt.X < 0)
            {
                startX = Math.Abs(tt.X);
                if (endWidth > screenWidth)
                {
                    endWidth = screenWidth + startX;
                    if (endWidth > mapImage.ViewWidth)
                        endWidth = mapImage.ViewWidth;
                }
            }
            else
            {
                if (endWidth + tt.X > screenWidth)
                    endWidth = screenWidth - tt.X;
            }

            if (tt.Y < 0)
            {
                startY = Math.Abs(tt.Y);
                if (endHeight > screenHeight)
                {
                    endHeight = screenHeight + startY;
                    if (endHeight > mapImage.ViewHeight)
                        endHeight = mapImage.ViewHeight;
                }
            }
            else
            {
                if (endHeight + tt.Y > screenHeight)
                    endHeight = screenHeight - tt.Y;
            }

            Point startPoint = new Point(startX, startY);
            Point endPoint = new Point(endWidth, endHeight);

            if (endWidth <= screenWidth && endHeight <= screenHeight)
            {
                ReloadImage(mapImage, startPoint, endPoint, 5);
            }
            else
            {
                int zoom = zoomLevel / 2;
                zoom = zoom > 4 ? 1 : 5 - zoom;
                ReloadImage(mapImage, startPoint, endPoint, zoom);
            }
        }

        private void ReloadImage(MapImage mapImage, Point start, Point end, int overview)
        {
            CanvasView.Children.Clear();

            Image image = ReadImage(mapImage, start, end, overview);
            image.Width = end.X - start.X;
            image.Height = end.Y - start.Y;

            Console.WriteLine($"imageStartPoint : {imageStartPoint.X}, {imageStartPoint.Y}");
            Console.WriteLine($"imageEndPoint : {imageEndPoint.X}, {imageEndPoint.Y}");

            Canvas.SetLeft(image, -((image.Width - (end.X + start.X)) / 2));
            Canvas.SetTop(image, -((image.Height - (end.Y + start.Y)) / 2));

            CanvasView.Children.Add(image);
        }

        private Image ReadImage(MapImage mapImage, Point startPoint, Point endPoint, int overview)
        {
            imageStartPoint = ScreenToImage(mapImage, startPoint);
            imageEndPoint = ScreenToImage(mapImage, endPoint);

            if (imageEndPoint.X > mapImage.ImageWidth)
                imageEndPoint.X = mapImage.ImageWidth;

            if (imageEndPoint.Y > mapImage.ImageHeight)
                imageEndPoint.Y = mapImage.ImageHeight;

            System.Drawing.Bitmap bitmap = gdalReader.GetBitmap((int)imageStartPoint.X, (int)imageStartPoint.Y,
                (int)imageEndPoint.X, (int)imageEndPoint.Y, overview);

            Image image = new Image();
            image.Source = ImageControl.BitmapToBitmapImage(bitmap, mapImage.ImageFormat);
            return image;
        }

        private void CanvasMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImages.Count > 0)
            {
                var tt = TranslateTransform;
                start = e.GetPosition(CanvasView);
                origin = new Point(tt.X, tt.Y);
                CanvasView.CaptureMouse();
            }
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (mapImages.Count > 0)
            {
                if (CanvasView.IsMouseCaptured)
                {
                    var tt = TranslateTransform;
                    Vector v = start - e.GetPosition(CanvasView);
                    tt.X -= v.X;
                    tt.Y -= v.Y;
                }

                var p = e.GetPosition(CanvasView);
                Point imagePoint = ScreenToImage(mapImages[0], p);
                Point lonlat = gdalReader.ImageToWorld(imagePoint.X, imagePoint.Y);

                string statusLine = $"Map({lonlat.X}, {lonlat.Y}), Image({imagePoint.X}, {imagePoint.Y}), Display({p.X}, {p.Y})";
                EventAggregator.MouseMoveEvent.Publish(statusLine);
            }
        }

        private void CanvasMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImages.Count > 0)
            {
                CanvasView.ReleaseMouseCapture();
            }
        }

        private void CanvasMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        public void ZoomFit()
        {
            if (mapImages.Count > 0)
            {
                Init();
                FitCanvas();
            }
        }

        public void ZoomIn()
        {

        }

        public void ZoomOut()
        {

        }

        public void SelectZoomToggle(bool isChecked)
        {

        }

        public void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public Point ScreenToImage(MapImage mapImage, Point point)
        {
            return new Point()
            {
                X = point.X / mapImage.ViewWidth * mapImage.ImageWidth,
                Y = point.Y / mapImage.ViewHeight * mapImage.ImageHeight
            };
        }

    }
}
