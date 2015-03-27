using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPLibrary : IApiEntity
    {
        public SPLibrary() { }

        public SPLibrary(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public int TotalCount { get; set; }

        public ApiList<SPListItem> Collection { get; set; }

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }
    }
}
