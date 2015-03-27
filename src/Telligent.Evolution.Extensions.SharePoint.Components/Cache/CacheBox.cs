
namespace Telligent.Evolution.Extensions.SharePoint.Components.Cache
{
    public class CacheBox<T>
    {
        public CacheBox() { }
        public CacheBox(T data)
        {
            Data = data;
        }

        public T Data { get; private set; }
    }
}
