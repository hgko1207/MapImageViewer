using ImageViewer.Domain;

namespace ImageViewer.Events
{
    public class EventAggregator
    {
        public static EventManager<string> MouseMoveEvent { set; get; } = new EventManager<string>();

        public static EventManager<int> ProgressEvent { set; get; } = new EventManager<int>();

        public static EventManager<MapImage> ImageOpenEvent { set; get; } = new EventManager<MapImage>();
    }
}
