namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class UserFieldMapping
    {
        public string ExternalUserFieldId { get; set; }

        public string InternalUserFieldId { get; set; }

        public SyncDirection SyncDirection { get; set; }
    }
}
