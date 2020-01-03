using ImageViewer.Domain;
using ImageViewer.Events;
using ImageViewer.Images;
using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageViewer.Utils
{
    public class GDALReader
    {
        private Dataset dataset;

        private int[] levels;

        private string filePath;

        public void Open(String filePath)
        {
            this.filePath = filePath;

            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset == null)
            {
                Console.WriteLine("Can't open " + filePath);
                Environment.Exit(-1);
            }

            levels = new int[] { 1, 2, 4, 8, 16, 32 };

            BuildOverview(levels);
        }

        /**
         * Downsampling 피라미드 형식으로 생성
         */
        private void BuildOverview(int[] levels)
        {

            if (dataset.BuildOverviews("NEAREST", levels, new Gdal.GDALProgressFuncDelegate(ProgressFunc), "Sample Data") != (int)CPLErr.CE_None)
            {
                Console.WriteLine("The BuildOverviews operation doesn't work");
                Environment.Exit(-1);
            }

            /* -------------------------------------------------------------------- */
            /*      Displaying the raster parameters                                */
            /* -------------------------------------------------------------------- */
            for (int iBand = 1; iBand <= dataset.RasterCount; iBand++)
            {
                Band band = dataset.GetRasterBand(iBand);
                Console.WriteLine("Band " + iBand + " :");
                Console.WriteLine("   DataType: " + band.DataType);
                Console.WriteLine("   Size (" + band.XSize + "," + band.YSize + ")");
                Console.WriteLine("   PaletteInterp: " + band.GetRasterColorInterpretation().ToString());

                for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                {
                    Band over = band.GetOverview(iOver);
                    Console.WriteLine("      OverView " + iOver + " :");
                    Console.WriteLine("         DataType: " + over.DataType);
                    Console.WriteLine("         Size (" + over.XSize + "," + over.YSize + ")");
                    Console.WriteLine("         PaletteInterp: " + over.GetRasterColorInterpretation().ToString());
                }
            }
            Console.WriteLine("Completed.");
        }

        public MapImage GetMapImageInfo()
        {
            MapImage mapImage = new MapImage(GetHeaderInfo(), filePath);
            Boundary ImageBoundary = null;
            if (GeometryControl.GetImageBoundary(dataset, out double minX, out double minY, out double maxX, out double maxY))
                ImageBoundary = new Boundary(minX, minY, maxX, maxY, 0, 0);

            mapImage.ImageBoundary = ImageBoundary;

            return mapImage;
        }

        /**
        * 헤더 생성
        */
        public HeaderInfo GetHeaderInfo()
        {
            HeaderInfo headerInfo = new HeaderInfo();
            headerInfo.FileName = Path.GetFileName(filePath);
            headerInfo.FileType = dataset.GetDriver().ShortName + "/" + dataset.GetDriver().LongName;
            headerInfo.Band = dataset.RasterCount;
            headerInfo.Width = dataset.RasterXSize;
            headerInfo.Height = dataset.RasterYSize;

            headerInfo.DataType = Gdal.GetDataTypeName(dataset.GetRasterBand(1).DataType);
            headerInfo.Description = dataset.GetDescription();

            SetMapInfo(headerInfo);

            return headerInfo;
        }

        private void SetMapInfo(HeaderInfo headerInfo)
        {
            string projection = dataset.GetProjectionRef();
            if (!string.IsNullOrEmpty(projection))
            {
                SpatialReference sr = new SpatialReference(projection);

                MapInfo mapInfo = new MapInfo();
                mapInfo.Projcs = sr.GetAttrValue("PROJCS", 0);
                mapInfo.Unit = sr.GetAttrValue("UNIT", 0);

                headerInfo.MapInfo = mapInfo;
            }
        }

        public Bitmap GetBitmap(int xOff, int yOff, int xSize, int ySize, int overview)
        {
            if (dataset.RasterCount == 1)
                return ReadGrayBitmap(xOff, yOff, xSize, ySize, overview);
            else
                return ReadRgbBitmap(xOff, yOff, xSize, ySize, overview);
        }

        private Bitmap ReadGrayBitmap(int xOffset, int yOffset, int xSize, int ySize, int width, int height, int overview)
        {
            Band band = dataset.GetRasterBand(1);
            if (overview > 0)
            {
                band = band.GetOverview(overview - 1);
            }

            int level = levels[overview - 1];
            xSize = xSize / level;
            ySize = ySize / level;
            int xOff = xOffset / level;
            int yOff = yOffset / level;

            int bandWidth = band.XSize;
            int bandWHeight = band.YSize;
            if (xOff + xSize > bandWidth)
            {
                xSize = bandWidth - xOff;
            }

            if (yOff + ySize > bandWHeight)
            {
                ySize = bandWHeight - yOff;
            }

            double[] minmax = new double[2];
            band.ComputeRasterMinMax(minmax, 0);
            double min = minmax[0];
            double max = minmax[1];
            double stretchRate = 255 / (max - min);

            int[] data = new int[width * height];
            band.ReadRaster(xOff, yOff, xSize, ySize, data, width, height, 0, 0);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            int stride = Math.Abs(bitmapData.Stride);
            byte[] bytes = new byte[height * stride];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int num = data[row * width + col];
                    byte value = (byte)((num - min) * stretchRate);
                    bytes[row * stride + col * 3] = value;
                    bytes[row * stride + col * 3 + 1] = value;
                    bytes[row * stride + col * 3 + 2] = value;
                    //bytes[row * stride + col * 4 + 3] = 255;
                }
            }

            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Bitmap ReadGrayBitmap(int xOffset, int yOffset, int xSize, int ySize, int overview)
        {
            Band band = dataset.GetRasterBand(1);
            if (overview > 0)
            {
                band = band.GetOverview(overview - 1);
            }

            int level = levels[overview - 1];
            int width = (xSize - xOffset) / level;
            int height = (ySize - yOffset) / level;
            int xOff = xOffset / level;
            int yOff = yOffset / level;

            int bandWidth = band.XSize;
            int bandWHeight = band.YSize;
            if (xOff + width > bandWidth)
            {
                width = bandWidth - xOff;
            }

            if (yOff + height > bandWHeight)
            {
                height = bandWHeight - yOff;
            }

            double[] minmax = new double[2];
            band.ComputeRasterMinMax(minmax, 0);
            double min = minmax[0];
            double max = minmax[1];
            double stretchRate = 255 / (max - min);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            //int stride = bitmapData.Stride;
            //IntPtr buf = bitmapData.Scan0;

            //band.ReadRaster(xOff, yOff, width, height, buf, width, height, DataType.GDT_Byte, 1, stride);

            int[] data = new int[width * height];
            band.ReadRaster(xOff, yOff, width, height, data, width, height, 0, 0);

            int stride = Math.Abs(bitmapData.Stride);
            byte[] bytes = new byte[height * stride];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int num = data[row * width + col];
                    byte value = (byte)((num - min) * stretchRate);
                    bytes[row * stride + col * 3] = value;
                    bytes[row * stride + col * 3 + 1] = value;
                    bytes[row * stride + col * 3 + 2] = value;
                    //bytes[row * stride + col * 4 + 3] = 255;
                }
            }

            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Bitmap ReadRgbBitmap(int xOffset, int yOffset, int xSize, int ySize, int overview)
        {
            int level = levels[overview - 1];

            Band redBand = dataset.GetRasterBand(3).GetOverview(overview - 1);
            Band greenBand = dataset.GetRasterBand(2).GetOverview(overview - 1);
            Band buleBand = dataset.GetRasterBand(1).GetOverview(overview - 1);

            int width = (xSize - xOffset) / level;
            int height = (ySize - yOffset) / level;
            int xOff = xOffset / level;
            int yOff = yOffset / level;

            int bandWidth = redBand.XSize;
            int bandWHeight = redBand.YSize;
            if (xOff + width > bandWidth)
            {
                width = bandWidth - xOff;
            }

            if (yOff + height > bandWHeight)
            {
                height = bandWHeight - yOff;
            }

            PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
            Bitmap bitmap = new Bitmap(width, height, pixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, pixelFormat);

            int stride = bitmapData.Stride;
            IntPtr buf = bitmapData.Scan0;

            redBand.ReadRaster(xOff, yOff, width, height, buf, width, height, DataType.GDT_Byte, 3, stride);
            greenBand.ReadRaster(xOff, yOff, width, height, buf + 1, width, height, DataType.GDT_Byte, 3, stride);
            buleBand.ReadRaster(xOff, yOff, width, height, buf + 2, width, height, DataType.GDT_Byte, 3, stride);

            return bitmap;
        }

        private Bitmap ReadRgbBitmap(int xOffset, int yOffset, int width, int height)
        {
            int[] bandMap = new int[4] { 0, 0, 0, 0 };
            int channelCount = 1;
            bool hasAlpha = false;
            bool isIndexed = false;
            int channelSize = 8;
            ColorTable colorTable = null;

            if (xOffset + width > dataset.RasterXSize)
            {
                width = dataset.RasterXSize - xOffset;
            }

            if (yOffset + height > dataset.RasterYSize)
            {
                height = dataset.RasterYSize - yOffset;
            }

            // Evaluate the bands and find out a proper image transfer format
            for (int i = 0; i < dataset.RasterCount; i++)
            {
                Band band = dataset.GetRasterBand(i + 1);
                if (Gdal.GetDataTypeSize(band.DataType) > 8)
                    channelSize = 16;

                switch (band.GetRasterColorInterpretation())
                {
                    case ColorInterp.GCI_AlphaBand:
                        channelCount = 4;
                        hasAlpha = true;
                        bandMap[3] = i + 1;
                        break;
                    case ColorInterp.GCI_BlueBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[0] = i + 1;
                        break;
                    case ColorInterp.GCI_RedBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[2] = i + 1;
                        break;
                    case ColorInterp.GCI_GreenBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[1] = i + 1;
                        break;
                    case ColorInterp.GCI_PaletteIndex:
                        colorTable = band.GetRasterColorTable();
                        isIndexed = true;
                        bandMap[0] = i + 1;
                        break;
                    case ColorInterp.GCI_GrayIndex:
                        isIndexed = true;
                        bandMap[0] = i + 1;
                        break;
                    default:
                        // we create the bandmap using the dataset ordering by default
                        if (i < 4 && bandMap[i] == 0)
                        {
                            if (channelCount < i)
                                channelCount = i;
                            bandMap[i] = i + 1;
                        }
                        break;
                }
            }

            // find out the pixel format based on the gathered information
            PixelFormat pixelFormat;
            DataType dataType;
            int pixelSpace;

            if (isIndexed)
            {
                pixelFormat = PixelFormat.Format8bppIndexed;
                dataType = DataType.GDT_Byte;
                pixelSpace = 1;
            }
            else
            {
                if (channelCount == 1)
                {
                    if (channelSize > 8)
                    {
                        pixelFormat = PixelFormat.Format16bppGrayScale;
                        dataType = DataType.GDT_Int16;
                        pixelSpace = 2;
                    }
                    else
                    {
                        pixelFormat = PixelFormat.Format24bppRgb;
                        channelCount = 3;
                        dataType = DataType.GDT_Byte;
                        pixelSpace = 3;
                    }
                }
                else
                {
                    if (hasAlpha)
                    {
                        if (channelSize > 8)
                        {
                            pixelFormat = PixelFormat.Format64bppArgb;
                            dataType = DataType.GDT_UInt16;
                            pixelSpace = 8;
                        }
                        else
                        {
                            pixelFormat = PixelFormat.Format32bppArgb;
                            dataType = DataType.GDT_Byte;
                            pixelSpace = 4;
                        }
                        channelCount = 4;
                    }
                    else
                    {
                        if (channelSize > 8)
                        {
                            pixelFormat = PixelFormat.Format48bppRgb;
                            dataType = DataType.GDT_UInt16;
                            pixelSpace = 6;
                        }
                        else
                        {
                            pixelFormat = PixelFormat.Format24bppRgb;
                            dataType = DataType.GDT_Byte;
                            pixelSpace = 3;
                        }
                        channelCount = 3;
                    }
                }
            }

            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            if (isIndexed)
            {
                if (colorTable != null)
                {
                    ColorPalette pal = bitmap.Palette;
                    for (int i = 0; i < colorTable.GetCount(); i++)
                    {
                        ColorEntry ce = colorTable.GetColorEntry(i);
                        pal.Entries[i] = Color.FromArgb(ce.c4, ce.c1, ce.c2, ce.c3);
                    }
                    bitmap.Palette = pal;
                }
                else
                {
                    ColorPalette pal = bitmap.Palette;
                    for (int i = 0; i < 255; i++)
                    {
                        pal.Entries[i] = Color.FromArgb(255, i, i, i);
                    }
                    bitmap.Palette = pal;
                }
            }

            // Use GDAL raster reading methods to read the image data directly into the Bitmap
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, pixelFormat);

            try
            {
                int stride = bitmapData.Stride;
                IntPtr buf = bitmapData.Scan0;

                dataset.ReadRaster(xOffset, yOffset, width, height, buf, width, height, dataType, channelCount, bandMap, pixelSpace, stride, 1);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public int ProgressFunc(double Complete, IntPtr Message, IntPtr Data)
        {
            EventAggregator.ProgressEvent.Publish((int)(Complete * 100));
            //Console.Write("Processing ... " + Complete * 100 + "% Completed.");
            //if (Message != IntPtr.Zero)
            //    Console.Write(" Message:" + Marshal.PtrToStringAnsi(Message));
            //if (Data != IntPtr.Zero)
            //    Console.Write(" Data:" + Marshal.PtrToStringAnsi(Data));

            //Console.WriteLine("");
            return 1;
        }

        public System.Windows.Point ImageToWorld(double x, double y)
        {
            double[] adfGeoTransform = new double[6];
            double[] p = new double[3];

            dataset.GetGeoTransform(adfGeoTransform);
            p[0] = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            p[1] = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;

            SpatialReference src = new SpatialReference("");
            string s = dataset.GetProjectionRef();
            src.ImportFromWkt(ref s);

            SpatialReference wgs84 = new SpatialReference("");
            wgs84.SetWellKnownGeogCS("WGS84");

            CoordinateTransformation ct = new CoordinateTransformation(src, wgs84);
            ct.TransformPoint(p);

            ct.Dispose();
            wgs84.Dispose();
            src.Dispose();

            return new System.Windows.Point(p[0], p[1]);
        }
    }
}
