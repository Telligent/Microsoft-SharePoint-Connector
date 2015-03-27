using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class SPPagedList<T> : PagedList<T>
    {
        public SPPagedList() : base() { }
        public SPPagedList(AdditionalInfo additionalInfo) : base(additionalInfo) { }
        public SPPagedList(Error error) : base(error) { }
        public SPPagedList(Warning warning) : base(warning) { }
        public SPPagedList(IEnumerable<T> items) : base(items.ToList()) { }
        public SPPagedList(IEnumerable<T> items, string pageInfo, int pageSize, int totalCount) :
            base(items.ToList())
         {
            PageInfo = pageInfo;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        /// <summary>
        /// Paging information that is used to generate the next page of data.
        /// </summary>
        public string PageInfo { get; set; }

        /// <summary>
        /// Index of the first item.
        /// </summary>
        public int FirstPosition { get { return PageIndex * PageSize + 1; } }

        /// <summary>
        /// Index of the last item.
        /// </summary>
        public int LastPosition { get { return PageIndex * PageSize + Count; } }
    }
}
