namespace ImageViewer.Events
{
    public class EventManager<T>
    {
        public delegate void EventHandler(T item);

        event EventHandler _Event;

        public void Subscribe(EventHandler handler)
        {
            _Event += handler;
        }

        public void UnSubscribe(EventHandler handler)
        {
            _Event -= handler;
        }

        public void Publish(T item)
        {
            _Event?.Invoke(item);
        }
    }
}
