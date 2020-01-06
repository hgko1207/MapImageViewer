using ImageViewer.Domain;
using ImageViewer.Events;
using ImageViewer.Utils;
using Microsoft.Win32;
using System;
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
        private MapImage mapImage;

        private MapToolMode mapToolMode = MapToolMode.Panning;

        public CanvasViewer()
        {
            InitializeComponent();
            InitEvent();
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
                CanvasView.Children.Clear();

                string fileName = fileDialog.FileName;

                gdalReader = new GDALReader();
                gdalReader.Open(fileName);
                mapImage = gdalReader.GetMapImageInfo();

                Init();
                FitCanvas();

                EventAggregator.ImageOpenEvent.Publish(mapImage);
            }
        }

        public void SaveExecuted()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png|JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            saveFileDialog.Title = "Save an Image File";
            if (saveFileDialog.ShowDialog() == true)
            {
                GdalUtil.SubsetImage(mapImage.FilePath, saveFileDialog.FileName, imageStartPoint, imageEndPoint);
            }
        }

        private void InitLoadImage()
        {
            CanvasView.Children.Clear();

            System.Drawing.Bitmap bitmap = gdalReader.GetBitmap(0, 0, mapImage.ImageWidth, mapImage.ImageHeight, 5);
            Image image = new Image();
            image.Source = ImageControl.BitmapToBitmapImage(bitmap, mapImage.ImageFormat);
            image.Width = mapImage.ViewWidth;
            image.Height = mapImage.ViewHeight;
            image.Stretch = Stretch.Fill;

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);

            CanvasView.Children.Add(image);
        }

        private void FitCanvas()
        {
            if (mapImage != null)
            {
                double imageWidth = mapImage.ImageWidth;
                double imageHeight = mapImage.ImageHeight;

                if (screenWidth / imageWidth < screenHeight / imageHeight)
                {
                    imageWidth = screenWidth;
                    imageHeight *= screenWidth / imageWidth;
                }
                else
                {
                    imageWidth *= screenHeight / imageHeight;
                    imageHeight = screenHeight;
                }

                mapImage.ViewWidth = imageWidth;
                mapImage.ViewHeight = imageHeight;

                InitLoadImage();
                CenterZoom();
            }
        }

        public void CenterZoom()
        {
            if (mapImage != null)
            {
                double imageWidth = mapImage.ViewWidth;
                double imageHeight = mapImage.ViewHeight;

                var tt = TranslateTransform;
                if (screenWidth > imageWidth)
                    tt.X = (screenWidth - imageWidth) / 2;
                else
                    tt.X = -(imageWidth - screenWidth) / 2;

                if (screenHeight > imageHeight)
                    tt.Y = (screenHeight - imageHeight) / 2;
                else
                    tt.Y = -(imageHeight - screenHeight) / 2;
            }
        }

        private void Zoom(int value, Point point)
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

            mapImage.ViewWidth *= rate;
            mapImage.ViewHeight *= rate;

            RedrawCanvas();
        }

        private void RedrawCanvas()
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
                {
                    endWidth = screenWidth - tt.X;
                }
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
                {
                    endHeight = screenHeight - tt.Y;
                }
                //if (endHeight > screenHeight)
                //    endHeight = screenHeight - tt.Y;
                //else if (endHeight == screenHeight)
                //    endHeight = endHeight - tt.Y;
            }

            Point startPoint = new Point(startX, startY);
            Point endPoint = new Point(endWidth, endHeight);

            Console.WriteLine($"---------------------------------------------------------");
            Console.WriteLine($"ZoomLevel : {zoomLevel}");
            //Console.WriteLine($"Translate : {tt.X}, {tt.Y}");
            //Console.WriteLine($"Screen : {screenWidth}, {screenHeight}");
            //Console.WriteLine($"Viewer : {mapImage.ViewWidth}, {mapImage.ViewHeight}");
            //Console.WriteLine($"Screen Start : {startX}, {startY}");
            //Console.WriteLine($"Screen end   : {endWidth}, {endHeight}");

            if (endWidth <= screenWidth && endHeight <= screenHeight)
            {
                ReloadImage(startPoint, endPoint, 5);
            }
            else
            {
                int zoom = zoomLevel / 2;
                zoom = zoom > 4 ? 1 : 5 - zoom;
                Console.WriteLine($"Zoom : {zoom}");
                ReloadImage(startPoint, endPoint, zoom);
            }
        }

        private void ReloadImage(Point start, Point end, int overview)
        {
            CanvasView.Children.Clear();

            Image image = ReadImage(start, end, overview);
            image.Width = end.X - start.X;
            image.Height = end.Y - start.Y;

            //Console.WriteLine($"image : {image.Width}, {image.Height}");
            //Console.WriteLine($"start : {start.X}, {start.Y}");
            //Console.WriteLine($"end : {end.X}, {end.Y}");
            //Console.WriteLine($"canvas : {-((image.Width - (end.X + start.X)) / 2)}, {-((image.Height - (end.Y + start.Y)) / 2)}");
            Console.WriteLine($"imageStartPoint : {imageStartPoint.X}, {imageStartPoint.Y}");
            Console.WriteLine($"imageEndPoint : {imageEndPoint.X}, {imageEndPoint.Y}");

            Canvas.SetLeft(image, -((image.Width - (end.X + start.X)) / 2));
            Canvas.SetTop(image, -((image.Height - (end.Y + start.Y)) / 2));

            CanvasView.Children.Add(image);
        }

        private Image ReadImage(Point startPoint, Point endPoint, int overview)
        {
            //int imageWidth = mapImage.ImageWidth;
            //int imageHeight = mapImage.ImageHeight;

            //int width = 0;
            //int height = 0;

            //if (screenWidth > screenHeight)
            //{
            //    width = (int)(((float)screenHeight / imageHeight) * imageWidth);
            //    height = (int)screenHeight;
            //}
            //else
            //{
            //    width = (int)screenWidth;
            //    height = (int)(((float)screenWidth / imageWidth) * imageHeight);
            //}

            imageStartPoint = ScreenToImage(startPoint);
            imageEndPoint = ScreenToImage(endPoint);

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

        private void CanvasMousWheel(object sender, MouseWheelEventArgs e)
        {
            if (mapImage != null)
            {
                Point point = e.GetPosition(CanvasView);
                Zoom(e.Delta, point);
            }
        }

        private void CanvasMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                rectStartPoint = e.GetPosition(CanvasView);

                var tt = TranslateTransform;
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                CanvasView.CaptureMouse();

                if (mapToolMode == MapToolMode.Panning)
                    Cursor = Cursors.Hand;
            }
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (mapImage != null)
            {
                if (CanvasView.IsMouseCaptured)
                {
                    switch (mapToolMode)
                    {
                        case MapToolMode.Panning:
                            var tt = TranslateTransform;
                            Vector v = start - e.GetPosition(this);
                            tt.X = origin.X - v.X;
                            tt.Y = origin.Y - v.Y;
                            RedrawCanvas();
                            break;
                        case MapToolMode.SelectZoom:
                            Image image = (Image)CanvasView.Children[0];
                            var position = e.GetPosition(CanvasView);
                            if (position.X < 0)
                                position.X = 0;
                            else if (position.X > image.Width)
                                position.X = image.Width;

                            if (position.Y < 0)
                                position.Y = 0;
                            else if (position.Y > image.Height)
                                position.Y = image.Height;

                            if (e.LeftButton == MouseButtonState.Pressed)
                            {
                                CanvasView.Children.Remove(drawRectangle);
                                Point point = new Point();

                                if (position.X - rectStartPoint.X > 0)
                                    point.X = rectStartPoint.X;
                                else
                                    point.X = position.X;

                                if (position.Y - rectStartPoint.Y > 0)
                                    point.Y = rectStartPoint.Y;
                                else
                                    point.Y = position.Y;

                                double width = Math.Abs(position.X - rectStartPoint.X);
                                double height = Math.Abs(position.Y - rectStartPoint.Y);

                                drawRectangle = CreateRectangle(point, width, height);
                                CanvasView.Children.Add(drawRectangle);
                            }
                            break;
                    }
                }

                var p = e.GetPosition(CanvasView);
                Point imagePoint = ScreenToImage(p);
                Point lonlat = gdalReader.ImageToWorld(imagePoint.X, imagePoint.Y);

                string statusLine = $"Map({lonlat.X}, {lonlat.Y}), Image({imagePoint.X}, {imagePoint.Y}), Display({p.X}, {p.Y})";
                EventAggregator.MouseMoveEvent.Publish(statusLine);
            }
        }

        private void CanvasMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                switch (mapToolMode)
                {
                    case MapToolMode.Panning:
                        Cursor = Cursors.Arrow;
                        break;
                    case MapToolMode.SelectZoom:
                        break;
                }
                CanvasView.ReleaseMouseCapture();
            }
        }

        private void CanvasMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                if (mapToolMode == MapToolMode.SelectZoom)
                {
                    CanvasView.Children.Remove(drawRectangle);
                }
            }
        }

        public void ZoomFit()
        {
            if (mapImage != null)
            {
                Init();
                FitCanvas();
            }
        }

        public void ZoomIn()
        {
            if (mapImage != null)
            {
                var image = (Image)CanvasView.Children[0];
                Point point = new Point(image.Width / 2, image.Height / 2);
                Zoom(1, point);
            }
        }

        public void ZoomOut()
        {
            if (mapImage != null)
            {
                var image = (Image)CanvasView.Children[0];
                Point point = new Point(image.Width / 2, image.Height / 2);
                Zoom(-1, point);
            }
        }

        public void SelectZoomToggle(bool isChecked)
        {
            if (isChecked)
            {
                Cursor = Cursors.Cross;
                mapToolMode = MapToolMode.SelectZoom;
            }
            else
            {
                Cursor = Cursors.Arrow;
                mapToolMode = MapToolMode.Panning;
                CanvasView.Children.Remove(drawRectangle);
            }
        }

        public void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mapImage != null)
            {
                ComboBox combo = sender as ComboBox;
                ComboBoxItem item = (ComboBoxItem)combo.SelectedItem;

                RotateTransform.CenterX = CanvasView.ActualWidth / 2;
                RotateTransform.CenterY = CanvasView.ActualHeight / 2;
                RotateTransform.Angle = Int32.Parse(item.Content.ToString());
            }
        }

        public Point ScreenToImage(Point point)
        {
            return new Point()
            {
                X = point.X / mapImage.ViewWidth * mapImage.ImageWidth,
                Y = point.Y / mapImage.ViewHeight * mapImage.ImageHeight
            };
        }

        private Rectangle CreateRectangle(Point point, double width, double height)
        {
            Rectangle rect = new Rectangle();
            rect.Stroke = Brushes.Red;

            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = Colors.Red;
            brush.Opacity = 0.2;
            rect.Fill = brush;

            rect.StrokeThickness = 3;
            rect.StrokeDashArray = DoubleCollection.Parse("4, 3");
            rect.Width = width;
            rect.Height = height;

            Canvas.SetLeft(rect, point.X);
            Canvas.SetTop(rect, point.Y);
            return rect;
        }
    }
}
