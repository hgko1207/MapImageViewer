using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ImageViewer.Utils
{
    public class ImageControl
    {
        public static BitmapSource BitmapToBitmapImage(Bitmap bitmap, ImageFormat imageFormat)
        {
            Graphics graphics = Graphics.FromImage(bitmap);
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap,
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            return bitmapSource;
        }

        public static Bitmap GetBitmap(byte[] bytes, int width, int height, PixelFormat pixelFormat)
        {
            Bitmap bitmap = new Bitmap(width, height, pixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, pixelFormat);
            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
    }
}
