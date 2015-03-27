using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPDocumentVersionList : List<SPDocumentVersion>
    {
        public int TotalCount { get; set; }
    }
}
