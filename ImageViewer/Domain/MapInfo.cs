using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Domain
{
    public class MapInfo
    {
        public string Projcs { get; set; }

        public double XPixelSize { get; set; }

        public double YPixelSize { get; set; }

        public string Unit { get; set; }
    }
}
