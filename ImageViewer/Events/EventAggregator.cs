namespace ImageViewer.Events
{
    public class EventAggregator
    {
        public static EventManager<string> MouseMoveEvent { set; get; } = new EventManager<string>();

        public static EventManager<int> ProgressEvent { set; get; } = new EventManager<int>();
    }
}
