using System.Drawing.Imaging;

namespace ImageViewer.Domain
{
    public class MapImage
    {
        /** 이미지 파일의 절대 경로 */
        public string FilePath { get; set; }

        public HeaderInfo HeaderInfo { get; set; }

        public ImageFormat ImageFormat { get; set; }

        public double ViewWidth { get; set; }

        public double ViewHeight { get; set; }

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public Boundary ImageBoundary { get; internal set; }

        public MapImage(HeaderInfo headerInfo, string filePath)
        {
            this.HeaderInfo = headerInfo;
            this.FilePath = filePath;
            this.ImageWidth = headerInfo.Width;
            this.ImageHeight = headerInfo.Height;

            ImageFormat imageFormat = ImageFormat.Bmp;
            if (filePath.ToLower().Contains(".png") || filePath.ToLower().Contains(".tif"))
                imageFormat = ImageFormat.Png;
            else if (filePath.ToLower().Contains(".jpg") || filePath.ToLower().Contains(".jpeg"))
                imageFormat = ImageFormat.Jpeg;

            this.ImageFormat = imageFormat;
        }
    }
}
