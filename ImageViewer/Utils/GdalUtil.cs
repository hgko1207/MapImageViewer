using OSGeo.GDAL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace ImageViewer.Utils
{
    public class GdalUtil
    {
        /* -------------------------------------------------------------------- */
        /*      Report "IMAGE_STRUCTURE" metadata.                              */
        /* -------------------------------------------------------------------- */
        public static string ReportImageStructureMetadata(Dataset dataset)
        {
            String interleave = "";

            string[] metadata = dataset.GetMetadata("IMAGE_STRUCTURE");
            if (metadata.Length > 0)
            {
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    interleave = metadata[iMeta];
                }
            }

            return interleave;
        }

        public static void SubsetImage(string srcPath, string dstPath, Point start, Point end, Gdal.GDALProgressFuncDelegate callback = null)
        {
            string subsetOptions = $"-of GTiff -srcwin {start.X} {start.Y} {end.X} {end.Y}";
            string[] options = subsetOptions.Split(' ');
            GdalTranslate(srcPath, dstPath, options, callback);
        }

        public static void SubsetImage2(string srcPath, string dstPath, Point start, Point end, Gdal.GDALProgressFuncDelegate callback = null)
        {
            string subsetOptions = $"-of GTiff -projwin {start.X} {start.Y} {end.X} {end.Y}";
            string[] options = subsetOptions.Split(' ');
            GdalTranslate(srcPath, dstPath, options, callback);
        }

        public static bool GdalTranslate(string srcPath, string dstPath, string[] options, Gdal.GDALProgressFuncDelegate callback = null)
        {
            Dataset result = null;
            using (Dataset inputDataset = Gdal.Open(srcPath, Access.GA_ReadOnly))
            {
                try
                {
                    result = Gdal.wrapper_GDALTranslate(dstPath, inputDataset, new GDALTranslateOptions(options), callback, null);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    return false;
                }
                finally
                {
                    result.Dispose();
                }
                return true;
            }
        }

        public static bool GdalWarp(string srcPath, string dstPath, string[] options, Gdal.GDALProgressFuncDelegate callback = null)
        {
            GdalConfiguration.ConfigureGdal();
            using (Dataset inputDataset = Gdal.Open(srcPath, Access.GA_ReadOnly))
            {
                IntPtr[] ptr = { Dataset.getCPtr(inputDataset).Handle };
                GCHandle gcHandle = GCHandle.Alloc(ptr, GCHandleType.Pinned);
                Dataset result = null;
                try
                {
                    SWIGTYPE_p_p_GDALDatasetShadow dss = new SWIGTYPE_p_p_GDALDatasetShadow(gcHandle.AddrOfPinnedObject(), false, null);
                    result = Gdal.wrapper_GDALWarpDestName(dstPath, 1, dss, new GDALWarpAppOptions(options), callback, null);
                }
                catch (Exception) { return false; }
                finally
                {
                    gcHandle.Free();
                    result.Dispose();
                }
            }
            return true;
        }
    }
}
