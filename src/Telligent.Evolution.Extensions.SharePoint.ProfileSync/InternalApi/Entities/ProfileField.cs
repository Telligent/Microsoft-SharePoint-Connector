namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    internal class ProfileField
    {
        public ProfileField(string name, bool importAvailable)
        {
            Name = name;
            ImportAvailable = importAvailable;
        }

        public ProfileField(string name, string title, bool importAvailable)
            : this(name, importAvailable)
        {
            Title = title;
        }

        public string Name { get; private set; }
        public string Title { get; set; }
        public bool ImportAvailable { get; private set; }
    }
}
