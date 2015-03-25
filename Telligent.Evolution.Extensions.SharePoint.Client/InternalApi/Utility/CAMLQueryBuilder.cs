using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class CAMLQueryBuilder
    {
        public static CamlQuery GetItem(int id, string[] viewFields)
        {
            return GetQuery(1, viewFields, String.Format(@"
                <Where>
                    <Eq>
                        <FieldRef Name='ID'/>
                        <Value Type='Counter'>{0}</Value>
                    </Eq>
                </Where>", id), true);
        }

        public static CamlQuery GetItem(Guid id, string[] viewFields)
        {
            return GetQuery(1, viewFields, String.Format(@"
                <Where>
                    <Eq>
                        <FieldRef Name='UniqueId'/>
                        <Value Type='Guid'>{0}</Value>
                    </Eq>
                </Where>", id), true);
        }

        public static CamlQuery GetItemByTitle(string title, string[] viewFields)
        {
            return GetQuery(1, viewFields, String.Format(@"
                <Where>
                    <Eq>
                        <FieldRef Name='Title'/>
                        <Value Type='Text'>{0}</Value>
                    </Eq>
                </Where>", title), true);
        }

        public static CamlQuery ListItems(string[] viewFields = null, int itemCount = 100)
        {
            return new CamlQuery
            {
                ViewXml = String.Concat(
                "<View Scope='RecursiveAll'>",
                    CreateViewFieldsSection(viewFields),
                    CreateQueryOptionsSection(itemCount),
                "</View>")
            };
        }

        public static CamlQuery ListItems(IEnumerable<int> ids, string[] viewFields = null, int itemCount = 100)
        {
            return GetQuery(itemCount, viewFields, GetWhereSection(ids.ToArray(), "ID", "Counter"));
        }

        public static CamlQuery ListItems(IEnumerable<Guid> ids, string[] viewFields = null, int itemCount = 100)
        {
            return GetQuery(itemCount, viewFields, GetWhereSection(ids.ToArray(), "UniqueId", "Guid"));
        }

        public static CamlQuery GetQuery(int itemCount, string[] viewFields, string querySectionXML, bool queryAllFoldersAndSubFolders = false)
        {
            return new CamlQuery
            {
                ViewXml = String.Concat(
                queryAllFoldersAndSubFolders ? "<View Scope='RecursiveAll'>" : "<View>",
                    CreateWhereQuerySection(querySectionXML),
                    CreateViewFieldsSection(viewFields),
                    CreateQueryOptionsSection(itemCount),
                "</View>")
            };
        }

        private static string CreateViewFieldsSection(IEnumerable<string> viewFields)
        {
            if (viewFields == null)
                return string.Empty;

            var viewFieldsSection = new StringBuilder();
            viewFieldsSection.Append("<ViewFields>");
            foreach (var field in viewFields)
            {
                viewFieldsSection.AppendFormat("<FieldRef Name='{0}' />", field);
            }
            viewFieldsSection.Append("</ViewFields>");
            return viewFieldsSection.ToString();
        }

        private static string CreateWhereQuerySection(string whereSection)
        {
            var whereQuerySection = new StringBuilder();
            whereQuerySection.Append("<Query>");
            whereQuerySection.Append(whereSection);
            whereQuerySection.Append("</Query>");
            return whereQuerySection.ToString();
        }

        private static string CreateQueryOptionsSection(int rowlimit)
        {
            var queryOptionsSection = new StringBuilder();
            queryOptionsSection.AppendFormat("<RowLimit>{0}</RowLimit>", rowlimit);
            return queryOptionsSection.ToString();
        }

        private static string GetWhereSection<T>(T[] itemsArr, string fieldName, string valueType)
        {
            if (itemsArr.Length == 0)
                return String.Empty;

            var query = new StringBuilder(EqualQuery(fieldName, itemsArr[0].ToString(), valueType));
            for (int i = 1; i < itemsArr.Length; i++)
            {
                query.Insert(0, "<Or>" + EqualQuery(fieldName, itemsArr[i].ToString(), valueType));
                query.Append("</Or>");
            }
            return String.Format(@"<Where>{0}</Where>", query);
        }

        private static string EqualQuery(string fieldName, string fieldValue, string valueType)
        {
            return String.Format("<Eq><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Eq>", fieldName, fieldValue, valueType);
        }
    }
}
