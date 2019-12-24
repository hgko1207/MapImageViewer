namespace ImageViewer.Domain
{
    public class Boundary
    {
        //degree
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        //pixel
        public double Top { get; set; }
        public double Left { get; set; }

        public Boundary()
        {
            MinX = -180;
            MaxX = 180;
            MinY = -90;
            MaxY = 90;

            Top = 0;
            Left = 0;
        }

        public Boundary(double minX, double minY, double maxX, double maxY, double top, double left)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;

            Top = top;
            Left = left;
        }

        public void CalculateMargin(Boundary canvasBoundary, double pixelPerDegreeX, double pixelPerDegreeY)
        {
            Left = (MinX - canvasBoundary.MinX) * pixelPerDegreeX;
            Top = (canvasBoundary.MaxY - MaxY) * pixelPerDegreeY;
        }
    }
}
