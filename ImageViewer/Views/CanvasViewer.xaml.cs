using ImageViewer.Domain;
using ImageViewer.Geometry;
using ImageViewer.Utils;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageViewer.Views
{
    public partial class CanvasViewer : UserControl
    {
        private AffineTransform initTransform = new AffineTransform();
        private AffineTransform transform = new AffineTransform();

        private double screenWidth;
        private double screenHeight;
        private Rectangle canvasArea;

        private GDALReader gdalReader;

        private MapImage mapImage;

        private Point origin;
        private Point start;

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
                canvasArea = new Rectangle((int)ActualWidth, (int)ActualHeight);
            };

            this.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            {
                screenWidth = ActualWidth;
                screenHeight = ActualHeight;
                canvasArea = new Rectangle((int)ActualWidth, (int)ActualHeight);
                FitCanvas();
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
                HeaderInfo headerInfo = gdalReader.GetHeaderInfo();
                mapImage = new MapImage(headerInfo, fileName);

                FitCanvas();
            }
        }

        public void SaveExecuted()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                //GdalUtil.SubsetImage(gdalReader.FileName, saveFileDialog.FileName, imageStartPoint, imageEndPoint);
            }
        }

        private void InitLoadImage()
        {

        }

        private void FitCanvas()
        {
            if (mapImage != null)
            {
                Rectangle imageBound = mapImage.Bounds;
                Rectangle destRect = canvasArea;
                double sx = (double)destRect.Width / imageBound.Width;
                double sy = (double)destRect.Height / imageBound.Height;
                double s = Math.Min(sx, sy);

                double dx = (double)destRect.Width / 2;
                double dy = (double)destRect.Height / 2;

                CenterZoom(dx, dy, s, new AffineTransform(), true);
            }
        }

        private void CenterZoom(double dx, double dy, double scale, AffineTransform af, bool init)
        {
            Console.WriteLine($"==============================================================");

            af.PreConcatenate(AffineTransform.GetTranslateInstance(-dx, -dy));
            af.PreConcatenate(AffineTransform.GetScaleInstance(scale, scale));
            af.PreConcatenate(AffineTransform.GetTranslateInstance(dx, dy));

            transform = af;

            if (init)
                SyncScrollBars();
            else
                ReloadImage();
        }

        private void SyncScrollBars()
        {
            AffineTransform af = transform;
            double sx = af.GetScaleX(), sy = af.GetScaleY();
            double tx = af.GetTranslateX(), ty = af.GetTranslateY();
            if (tx > 0)
                tx = 0;
            if (ty > 0)
                ty = 0;

            Rectangle imageBound = mapImage.Bounds;
            int cw = canvasArea.Width, ch = canvasArea.Height;

            double imageWidth = imageBound.Width * sx;
            double imageHeight = imageBound.Height * sy;
            if (imageWidth > cw)
            { /* image is wider than client area */
                if (((int)-tx) > imageWidth - cw)
                    tx = cw - imageWidth;
            }
            else
            { /* image is narrower than client area */
                tx = (cw - imageWidth) / 2; // center if too small.
            }

            if (imageHeight > ch)
            { /* image is higher than client area */
                if (((int)-ty) > imageHeight - ch)
                    ty = ch - imageHeight;
            }
            else
            { /* image is less higher than client area */
                ty = (ch - imageHeight) / 2; // center if too small.
            }

            /* update transform. */
            AffineTransform scale = AffineTransform.GetScaleInstance(sx, sy);
            if (tx < 0)
                tx = 0;
            AffineTransform translate = AffineTransform.GetTranslateInstance(tx, ty);

            Console.WriteLine($"Translate : {tx}, {ty}");
            Console.WriteLine($"Scale : {sx}, {sy}");

            af = scale;
            af.PreConcatenate(translate);

            transform = af;
            initTransform = af;

            ReloadImage();
        }

        private void ReloadImage()
        {
            CanvasView.Children.Clear();

            Rectangle clientRect = canvasArea;
            Rectangle imageRect = SWT2DUtil.InverseTransformRect(transform, clientRect);
            imageRect = imageRect.Intersection(mapImage.Bounds);
            Rectangle destRect = SWT2DUtil.TransformRect(transform, imageRect, clientRect);

            Console.WriteLine($"clientRect : {clientRect.Width}, {clientRect.Height}");
            Console.WriteLine($"imageRect : {imageRect.X}, {imageRect.Y}, {imageRect.Width}, {imageRect.Height}");
            Console.WriteLine($"destRect : {destRect.X}, {destRect.Y}, {destRect.Width}, {destRect.Height}");

            Image image = ReadImage(mapImage, clientRect, imageRect);
            image.Width = destRect.Width;

            var height = destRect.Height;
            if (height > clientRect.Height)
            {
                height = clientRect.Height;
            }
            image.Height = height;
            image.Stretch = Stretch.Fill;

            Canvas.SetLeft(image, destRect.X);
            int top = destRect.Y;
            if (destRect.Y < 0)
            {
                top = 0;
            }
            Canvas.SetTop(image, top);

            CanvasView.Children.Add(image);
        }

        private Image ReadImage(MapImage mapImage, Rectangle clientRect, Rectangle imageRect)
        {
            int imageWidth = mapImage.HeaderInfo.Width;
            int imageHeight = mapImage.HeaderInfo.Height;

            int windowWidth = clientRect.Width;
            int windowHeight = clientRect.Height;

            int width = 0;
            int height = 0;

            if (windowWidth > windowHeight)
            {
                width = (int)(((float)windowHeight / imageHeight) * imageWidth);
                height = windowHeight;
            }
            else
            {
                width = windowWidth;
                height = (int)(((float)windowWidth / imageWidth) * imageHeight);
            }

            System.Drawing.Bitmap bitmap = gdalReader.CreateBitmap(imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height, width, height, 4);

            Console.WriteLine($"Bitmap : {bitmap.Width}, {bitmap.Height}");

            Image image = new Image();
            image.Source = ImageControl.BitmapToBitmapImage(bitmap, mapImage.ImageFormat);

            return image;
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mapImage != null)
            {
                Point point = e.GetPosition(CanvasView);

                double sx = initTransform.GetScaleX();
                double sy = initTransform.GetScaleY();
                double tx = initTransform.GetTranslateX();
                double ty = initTransform.GetTranslateY();

                //double zoom = e.Delta > 0 ? .2 : -.2;

                //Console.WriteLine($"======================================================");
                //Console.WriteLine($"Point : {point.X}, {point.Y}");
                //Console.WriteLine($"Scale : {sx}, {sy}");
                //Console.WriteLine($"Translate : {tx}, {ty}");

                //double rate = 0.9;
                //if (e.Delta > 0) //Zoom in, 확대
                //    rate = 1 / rate;
                //double offsetX = Math.Abs(point.X * (1 - rate));
                //double offsetY = Math.Abs(point.Y * (1 - rate));

                //Console.WriteLine($"offset : {offsetX}, {offsetY}");

                //if (e.Delta > 0) //확대
                //{
                //    tx -= offsetX;
                //    ty -= offsetY;
                //}
                //else
                //{
                //    tx += offsetX;
                //    ty += offsetY;
                //}

                //sx *= rate;
                //sy *= rate;

                //AffineTransform af = transform;
                //af.PreConcatenate(AffineTransform.GetTranslateInstance(-tx, -tx));
                //af.PreConcatenate(AffineTransform.GetScaleInstance(rate, rate));
                //af.PreConcatenate(AffineTransform.GetTranslateInstance(tx, tx));

                //transform = af;

                //Console.WriteLine($"Translate2 : {transform.GetTranslateX()}, {transform.GetTranslateY()}");
                //Console.WriteLine($"Scale2 : {transform.GetScaleX()}, {transform.GetScaleY()}");

                //SyncScrollBars();

                //Console.WriteLine($"Translate3 : {transform.GetTranslateX()}, {transform.GetTranslateY()}");
                //Console.WriteLine($"Scale3 : {transform.GetScaleX()}, {transform.GetScaleY()}");

                var st = ScaleTransform;
                var tt = TranslateTransform;

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                double abosuluteX = point.X * st.ScaleX + tt.X;
                double abosuluteY = point.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = abosuluteX - point.X * st.ScaleX;
                tt.Y = abosuluteY - point.Y * st.ScaleY;

                Console.WriteLine($"======================================================");
                Console.WriteLine($"Point : {point.X}, {point.Y}");
                Console.WriteLine($"Scale : {sx}, {sy}");
                Console.WriteLine($"Translate : {tx}, {ty}");

                Console.WriteLine($"Scale2 : {st.ScaleX}, {st.ScaleY}");
                Console.WriteLine($"Translate2 : {tt.X}, {tt.Y}");

                AffineTransform af = initTransform;
                //AffineTransform scale = AffineTransform.GetScaleInstance(sx * st.ScaleX, sy * st.ScaleY);
                AffineTransform translate = AffineTransform.GetTranslateInstance(tx + tt.X, ty + tt.Y);

                af = translate;
                af.PreConcatenate(translate);

                transform = af;

                Console.WriteLine($"Scale3 : {transform.GetScaleX()}, {transform.GetScaleY()}");
                Console.WriteLine($"Translate3 : {transform.GetTranslateX()}, {transform.GetTranslateY()}");

                ReloadImage();
            }
        }

        private void CanvasMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                var tt = TranslateTransform;
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                CanvasView.CaptureMouse();
                this.Cursor = Cursors.Hand;
            }
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (mapImage != null)
            {
                if (CanvasView.IsMouseCaptured)
                {
                    var tt = TranslateTransform;
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;

                    //RedrawCanvas();
                }
            }
        }

        private void CanvasMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                this.Cursor = Cursors.Arrow;
                CanvasView.ReleaseMouseCapture();
            }
        }

        private void CanvasMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {

            }
        }

        public void ZoomFit()
        {
            if (mapImage != null)
            {
                // reset zoom
                var st = ScaleTransform;
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = TranslateTransform;
                tt.X = 0.0;
                tt.Y = 0.0;

                FitCanvas();
            }
        }
    }
}
