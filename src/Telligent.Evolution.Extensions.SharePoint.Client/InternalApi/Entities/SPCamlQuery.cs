using System;
using System.Linq;
using System.Globalization;
using System.Text;
using Telligent.Evolution.Extensions.SharePoint.Components;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class SPCamlQueryUtility
    {
        internal static SP.CamlQuery ToSPCamlRequest(this SPCamlQuery query, string pagingInfo = null)
        {
            var camlQuery = new SP.CamlQuery
            {
                DatesInUtc = query.DatesInUtc,
                ViewXml = query.ViewXml
            };

            if (!string.IsNullOrEmpty(query.FolderPath))
            {
                camlQuery.FolderServerRelativeUrl = query.FolderPath;
            }

            if (!string.IsNullOrEmpty(pagingInfo))
            {
                camlQuery.ListItemCollectionPosition = new SP.ListItemCollectionPosition { PagingInfo = pagingInfo };
            }

            return camlQuery;
        }
    }

    internal class SPCamlQuery
    {
        private static readonly string[] defaultViewFields = new []{ "UniqueId" };

        public enum ViewScope
        {
            Default = 0,
            Recursive = 1,
            RecursiveAll = 2,
            FilesOnly = 3,
        }

        private SPCamlQuery() { }

        public SPCamlQuery(int rowLimit)
            : this()
        {
            RowLimit = rowLimit;
        }

        public SPCamlQuery(SPCamlQuery query)
            : this()
        {
            if (query == null) return;

            DatesInUtc = query.DatesInUtc;
            FolderPath = query.FolderPath;
            RowLimit = query.RowLimit;
            Scope = query.Scope;
            SortBy = query.SortBy;
            SortOrder = query.SortOrder;
            GroupBy = query.GroupBy;
            GroupOrder = query.GroupOrder;
            if (query.ViewFields != null)
            {
                ViewFields = (string[])query.ViewFields.Clone();
            }
            Where = query.Where;
        }

        public bool DatesInUtc { get; set; }
        public string FolderPath { get; set; }
        public int RowLimit { get; set; }
        public ViewScope Scope { get; set; }
        public string SortBy { get; set; }
        public SortOrder SortOrder { get; set; }
        public string GroupBy { get; set; }
        public SortOrder GroupOrder { get; set; }
        public string[] ViewFields { get; set; }
        public string Where { get; set; }

        public string ViewXml
        {
            get
            {
                var queryXml = new StringBuilder();
                if (Scope == ViewScope.Default)
                {
                    queryXml.Append("<View>");
                }
                else
                {
                    queryXml.AppendFormat("<View Scope='{0}'>", Enum.GetName(typeof(ViewScope), Scope));
                }

                AppendQuery(queryXml);
                AppendViewFields(queryXml);
                AppendRowLimit(queryXml);

                queryXml.Append("</View>");
                return queryXml.ToString();
            }
        }

        private void AppendQuery(StringBuilder queryXml)
        {
            queryXml.Append("<Query>");

            if (!string.IsNullOrEmpty(Where))
            {
                string where = Where;
                int startIndex = Where.IndexOf("<OrderBy>", StringComparison.InvariantCulture);
                int lastIndex = Where.LastIndexOf("</OrderBy>", StringComparison.Ordinal) + "</OrderBy>".Length;
                if (startIndex >= 0 && lastIndex > 0 && !string.IsNullOrEmpty(SortBy))
                {
                    where = where.Remove(startIndex, lastIndex - startIndex);
                }
                queryXml.Append(where);
            }

            if (!string.IsNullOrEmpty(SortBy))
            {
                queryXml.AppendFormat(@"<OrderBy><FieldRef Name='{0}' Ascending='{1}' /></OrderBy>",
                    SortBy, (SortOrder == SortOrder.Ascending).ToString(CultureInfo.InvariantCulture).ToUpperInvariant());
            }

            if (!string.IsNullOrEmpty(GroupBy))
            {
                queryXml.AppendFormat(@"<GroupBy><FieldRef Name='{0}' Ascending='{1}'/></GroupBy>",
                    GroupBy, (GroupOrder == SortOrder.Ascending).ToString(CultureInfo.InvariantCulture).ToUpperInvariant());
            }

            queryXml.Append("</Query>");
        }

        private void AppendViewFields(StringBuilder queryXml)
        {
            if (ViewFields == null || ViewFields.Length <= 0)
                return;

            queryXml.Append("<ViewFields>");
            foreach (var fieldName in ViewFields.Union(defaultViewFields))
            {
                queryXml.AppendFormat("<FieldRef Name='{0}' />", fieldName);
            }
            queryXml.Append("</ViewFields>");
        }

        private void AppendRowLimit(StringBuilder queryXml)
        {
            if (RowLimit > 0)
            {
                queryXml.AppendFormat("<RowLimit>{0}</RowLimit>", RowLimit);
            }
        }

        public override int GetHashCode()
        {
            return DatesInUtc ? 1 : 0
                ^ (!string.IsNullOrEmpty(FolderPath) ? FolderPath.GetHashCode() : 0)
                ^ RowLimit
                ^ (int)Scope
                ^ (!string.IsNullOrEmpty(SortBy) ? SortBy.GetHashCode() : 0)
                ^ (int)SortOrder
                ^ (!string.IsNullOrEmpty(GroupBy) ? GroupBy.GetHashCode() : 0)
                ^ (int)GroupOrder
                ^ (ViewFields != null && ViewFields.Length > 0 ? String.Join(",", ViewFields).GetHashCode() : 0)
                ^ (!string.IsNullOrEmpty(Where) ? Where.GetHashCode() : 0);
        }
    }
}
