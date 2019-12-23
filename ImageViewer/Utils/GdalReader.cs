using ImageViewer.Domain;
using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    
            return headerInfo;
        }

        public int ProgressFunc(double Complete, IntPtr Message, IntPtr Data)
        {
            //EventAggregator.ProgressEvent.Publish((int)(Complete * 100));
            //Console.Write("Processing ... " + Complete * 100 + "% Completed.");
            //if (Message != IntPtr.Zero)
            //    Console.Write(" Message:" + Marshal.PtrToStringAnsi(Message));
            //if (Data != IntPtr.Zero)
            //    Console.Write(" Data:" + Marshal.PtrToStringAnsi(Data));

            //Console.WriteLine("");
            return 1;
        }
    }
}
