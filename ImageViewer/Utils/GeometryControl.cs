using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Diagnostics;

namespace ImageViewer.Images
{
    public enum EPSGType
    {
        OGC_WKT,
        PROJ4,
        EPSG_NUM
    }

    public class GeometryControl
    {
        public static bool GetImageBoundary(Dataset dataset, out double minX, out double minY, out double maxX, out double maxY)
        {
            string wkt = "";
            minX = double.MaxValue;
            minY = double.MaxValue;
            maxX = double.MinValue;
            maxY = double.MinValue;

            try
            {
                wkt = GetSwath(dataset);
                string coordWKT = dataset.GetProjectionRef();
                Console.WriteLine(coordWKT);
                if (wkt == "" || String.IsNullOrEmpty(coordWKT))
                    return false;

                TransformWKT(coordWKT, 4326, EPSGType.OGC_WKT, EPSGType.EPSG_NUM, ref wkt);

                string[] coords = wkt.Split('(')[2].Split(')')[0].Split(',');
                foreach (string coord in coords)
                {
                    double.TryParse(coord.Split(' ')[0], out double x);
                    double.TryParse(coord.Split(' ')[1], out double y);

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
        }

        private static string GetSwath(Dataset dataset)
        {
            string wkt = "";
            try
            {
                double[] geoTransform = new double[6];

                dataset.GetGeoTransform(geoTransform);

                var ulX = geoTransform[0];
                var ulY = geoTransform[3];
                var xRes = geoTransform[1];
                var yRes = geoTransform[5];

                var lrX = ulX + dataset.RasterXSize * xRes;
                var lrY = ulY + dataset.RasterYSize * yRes;

                wkt += "POLYGON((";
                wkt += ulX + " " + ulY + ",";
                wkt += ulX + " " + lrY + ",";
                wkt += lrX + " " + lrY + ",";
                wkt += lrX + " " + ulY + ",";
                wkt += ulX + " " + ulY + "))";
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return wkt;
        }

        private static string GetCoordWKT(string sourcePath)
        {
            string coord = "";
            try
            {
                GdalConfiguration.ConfigureGdal();
                GdalConfiguration.ConfigureOgr();
                using (Dataset inputDataset = Gdal.Open(sourcePath, Access.GA_ReadOnly))
                {
                    coord = inputDataset.GetProjectionRef();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return coord;
        }

        private static void TransformWKT(object srcProj, object dstProj, EPSGType srcType, EPSGType dstType, ref string wkt)
        {
            try
            {
                GdalConfiguration.ConfigureOgr();
                SpatialReference src = new SpatialReference("");
                switch (srcType)
                {
                    case EPSGType.OGC_WKT:
                        string srcProj_str = srcProj.ToString();
                        src.ImportFromWkt(ref srcProj_str);
                        break;
                    case EPSGType.PROJ4:
                        src.ImportFromProj4(srcProj.ToString());
                        break;
                    case EPSGType.EPSG_NUM:
                        src.ImportFromEPSG((int)srcProj);
                        break;
                }

                SpatialReference dst = new SpatialReference("");
                switch (dstType)
                {
                    case EPSGType.OGC_WKT:
                        string dstProj_str = dstProj.ToString();
                        dst.ImportFromWkt(ref dstProj_str);
                        break;
                    case EPSGType.PROJ4:
                        dst.ImportFromProj4(dstProj.ToString());
                        break;
                    case EPSGType.EPSG_NUM:
                        dst.ImportFromEPSG((int)dstProj);
                        break;
                }

                CoordinateTransformation coordinate = Osr.CreateCoordinateTransformation(src, dst);
                string wktType = wkt.Split('(')[0];
                wkt = wkt.Split('(')[2].Split(')')[0];
                string[] splitWKT = wkt.Split(',');
                double[] xPoints = new double[splitWKT.Length];
                double[] yPoints = new double[splitWKT.Length];
                for (int i = 0; i < splitWKT.Length; i++)
                {
                    double.TryParse(splitWKT[i].Split(' ')[0], out xPoints[i]);
                    double.TryParse(splitWKT[i].Split(' ')[1], out yPoints[i]);
                }
                coordinate.TransformPoints(splitWKT.Length, xPoints, yPoints, null);

                wkt = wktType + "((";
                for (int i = 0; i < xPoints.Length; i++)
                {
                    wkt += xPoints[i] + " " + yPoints[i] + ",";
                }
                wkt = wkt.Substring(0, wkt.Length - 1);
                wkt += "))";
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
