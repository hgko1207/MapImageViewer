using ImageViewer.Geometry;
using System.Drawing.Imaging;

namespace ImageViewer.Domain
{
    public class MapImage
    {
        /** 이미지 파일의 절대 경로 */
        public string FilePath { get; set; }

        public HeaderInfo HeaderInfo { get; set; }

        public Rectangle Bounds { get; set; }

        public ImageFormat ImageFormat { get; set; }

        public double ViewWidth { get; set; }

        public double ViewHeight { get; set; }

        public MapImage(HeaderInfo headerInfo, string filePath)
        {
            this.HeaderInfo = headerInfo;
            this.FilePath = filePath;

            ImageFormat imageFormat = ImageFormat.Bmp;
            if (filePath.ToLower().Contains(".png") || filePath.ToLower().Contains(".tif"))
                imageFormat = ImageFormat.Png;
            else if (filePath.ToLower().Contains(".jpg") || filePath.ToLower().Contains(".jpeg"))
                imageFormat = ImageFormat.Jpeg;

            this.ImageFormat = imageFormat;

            this.Bounds = new Rectangle(0, 0, headerInfo.Width, headerInfo.Height);
        }
    }
}
