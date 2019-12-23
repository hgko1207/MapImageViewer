namespace ImageViewer.Domain
{
    public class HeaderInfo
    {
        public string FileName { get; set; }

        public string FileType { get; set; }

        /** 밴드 수 */
        public int Band { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string DataType { get; set; }

        public string Interleave { get; set; }

        public string Description { get; set; }

        public MapInfo MapInfo { get; set; }
    }
}
