using ImageViewer.Geometry;
using ImageViewer.Geometry.Model;
using System;
using System.Diagnostics;

namespace ImageViewer.Utils
{
    public class SWT2DUtil
    {
        /**
	 * Given an arbitrary rectangle, get the rectangle with the given transform.
	 * The result rectangle is positive width and positive height.
	 * @param af AffineTransform
	 * @param src source rectangle
	 * @return rectangle after transform with positive width and height
	 */
        public static Rectangle TransformRect(AffineTransform af, Rectangle src)
        {
            Rectangle dest = new Rectangle(0, 0, 0, 0);
            src = AbsRect(src);

            Point p = new Point(src.X, src.X);
            p = TransformPoint(af, p);

            dest.X = p.X;
            dest.Y = p.Y;
            dest.Width = (int)Math.Round(src.Width * af.GetScaleX());
            dest.Height = (int)Math.Round(src.Width * af.GetScaleY());

            return dest;
        }

        /**
         * Given an arbitrary rectangle, get the rectangle with the given transform.
         * The result rectangle is positive width and positive height.
         * @param af AffineTransform
         * @param src source rectangle
         * @return rectangle after transform with positive width and height
         */
        public static Rectangle TransformRect(AffineTransform af, Rectangle src, Rectangle clientRect)
        {
            Rectangle dest = new Rectangle(0, 0, 0, 0);
            src = AbsRect(src);

            Point p = new Point(src.X, src.X);
            p = TransformPoint(af, p);

            dest.X = p.X;
            dest.Y = p.Y;

            dest.Width = clientRect.Width - dest.X * 2;
            dest.Height = clientRect.Height - dest.Y * 2;

            return dest;
        }

        /**
         * Given an arbitrary rectangle, get the rectangle with the inverse given transform.
         * The result rectangle is positive width and positive height.
         * @param af AffineTransform
         * @param src source rectangle
         * @return rectangle after transform with positive width and height
         */
        public static Rectangle InverseTransformRect(AffineTransform af, Rectangle src)
        {
            Rectangle dest = new Rectangle(0, 0, 0, 0);
            src = AbsRect(src);

            Point p = new Point(src.X, src.Y);
            p = InverseTransformPoint(af, p);

            dest.X = p.X;
            dest.Y = p.Y;
            dest.Width = (int)(src.Width / af.GetScaleX());
            dest.Height = (int)(src.Height / af.GetScaleY());
            return dest;
        }

        /**
         * Given an arbitrary point, get the point with the given transform.
         * @param af affine transform
         * @param p1 point to be transformed
         * @return point after tranform
         */
        public static Point TransformPoint(AffineTransform af, Point pt)
        {
            Point src = new Point(pt.X, pt.Y);
            try
            {
                Point dest = af.Transform(src, null);
                return new Point((int)Math.Round((double)dest.GetX()), (int)Math.Round((double)dest.GetY()));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return new Point(0, 0);
            }
        }

        /**
         * Given an arbitrary point, get the point with the inverse given transform.
         * @param af AffineTransform
         * @param pt source point
         * @return point after transform
         */
        public static Point InverseTransformPoint(AffineTransform af, Point pt)
        {
            Point src = new Point(pt.X, pt.Y);
            try
            {
                Point dest = af.InverseTransform(src, null);
                return new Point((int)Math.Round((double)dest.GetX()), (int)Math.Round((double)dest.GetY()));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return new Point(0, 0);
            }
        }

        public static Point InverseTransformPointFloat(AffineTransform af, Point pt)
        {
            Point src = new Point(pt.X, pt.Y);
            try
            {
                return af.InverseTransform(src, null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return new Point(0, 0);
            }
        }

        /**
         * Given arbitrary rectangle, return a rectangle with upper-left 
         * start and positive width and height.
         * @param src source rectangle
         * @return result rectangle with positive width and height
         */
        public static Rectangle AbsRectNew(Rectangle src)
        {
            Rectangle dest = new Rectangle(0, 0, 0, 0);
            if (src.Width < 0)
            {
                dest.X = src.X + src.Width + 1;
                dest.Width = -src.Width;
            }
            else
            {
                dest.X = src.X;
                dest.Width = src.Width;
            }
            if (src.Height < 0)
            {
                dest.Y = src.Y + src.Height + 1;
                dest.Height = -src.Height;
            }
            else
            {
                dest.Y = src.Y;
                dest.Height = src.Height;
            }
            return dest;
        }

        /**
         * Given arbitrary rectangle, return a rectangle with upper-left 
         * start and positive width and height.
         * @param src source rectangle
         * @return result rectangle with positive width and height
         */
        public static Rectangle AbsRect(Rectangle src)
        {
            int x, y, width, height;

            if (src.Width < 0)
            {
                x = src.X + src.Width + 1;
                width = -src.Width;
            }
            else
            {
                x = src.X;
                width = src.Width;
            }
            if (src.Height < 0)
            {
                y = src.Y + src.Height + 1;
                height = -src.Height;
            }
            else
            {
                y = src.Y;
                height = src.Height;
            }

            src.X = x;
            src.Y = y;
            src.Width = width;
            src.Height = height;

            return src;
        }

        /**
         * 두 점 사이의 거리를 구한다. 
         */
        public static int LineCalculate(Point start, Point end)
        {
            double result = Math.Sqrt(Math.Pow(Math.Abs(start.X - end.X), 2)
                    + Math.Pow(Math.Abs(start.Y - end.Y), 2));
            return (int)result;
        }

        public static int[] TransformPoints(AffineTransform transform, int[] polygons)
        {
            for (int i = 0; i < polygons.Length - 1; i = i + 2)
            {
                Point p = new Point(polygons[i], polygons[i + 1]);
                p = TransformPoint(transform, p);

                polygons[i] = p.X;
                polygons[i + 1] = p.Y;
            }

            return polygons;
        }
    }
}
