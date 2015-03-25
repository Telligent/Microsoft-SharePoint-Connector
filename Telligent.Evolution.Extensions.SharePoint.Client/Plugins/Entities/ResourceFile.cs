namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Entities
{
    internal abstract class ResourceFile
    {
        protected ResourceFile(string assemblyName, string path)
        {
            Path = string.Concat(assemblyName, ".", path);
            Init(path);
        }

        protected abstract void Init(string path);

        public string Path { get; protected set; }
        public string Name { get; protected set; }
        public string Theme { get; protected set; }
    }

}
