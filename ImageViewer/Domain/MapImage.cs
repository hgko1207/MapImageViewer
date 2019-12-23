using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageViewer.Domain
{
    public class MapImage
    {
        /** 이미지 파일의 절대 경로 */
        public string FilePath { get; set; }

        public HeaderInfo HeaderInfo { get; set; }

        public Rect Bounds { get; set; }

        public ImageFormat ImageFormat { get; set; }

        public MapImage(HeaderInfo headerInfo, string filePath)
        {
            this.HeaderInfo = headerInfo;
            this.FilePath = filePath;

            this.Bounds = new Rect(0, 0, headerInfo.Width, headerInfo.Height);
        }
    }
}
