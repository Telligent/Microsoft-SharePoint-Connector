using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public static class ApiEntityQueryableExtension
    {
        #region Library

        public static IEnumerable<Library> Order(this IEnumerable<Library> collection, Library.SortBy sortBy, string sortOrder)
        {
            return Library.Order(collection, sortBy, sortOrder);
        }

        public static IEnumerable<Library> Order(this IEnumerable<Library> collection, string sortBy, string sortOrder)
        {
            var sortByOption = Library.SortBy.Name;
            Enum.TryParse(sortBy, out sortByOption);
            return Library.Order(collection, sortByOption, sortOrder);
        }

        #endregion

        #region List

        public static IEnumerable<SPList> Order(this IEnumerable<SPList> collection, SPList.SortBy sortBy, string sortOrder)
        {
            return SPList.Order(collection, sortBy, sortOrder);
        }

        public static IEnumerable<SPList> Order(this IEnumerable<SPList> collection, string sortBy, string sortOrder)
        {
            var sortByOption = SPList.SortBy.Title;
            Enum.TryParse(sortBy, out sortByOption);
            return SPList.Order(collection, sortByOption, sortOrder);
        }

        #endregion
    }
}
