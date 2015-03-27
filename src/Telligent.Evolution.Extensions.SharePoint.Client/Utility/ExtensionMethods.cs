using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public static class ExtensionMethods
    {
        public static SP.List ToList(this SPContext clientContext, Guid listId)
        {
            return clientContext.Web.Lists.GetById(listId);
        }

        public static SP.List ToList(this SPContext clientContext, string listId)
        {
            return clientContext.Web.Lists.GetById(Guid.Parse(listId));
        }

        public static SP.File ToFile(this SPContext clientContext, Guid listId, int itemId)
        {
            return clientContext.Web.Lists.GetById(listId).GetItemById(itemId).File;
        }

        public static SP.File ToFile(this SPContext clientContext, Guid listId, string itemId)
        {
            return clientContext.Web.Lists.GetById(listId).GetItemById(itemId).File;
        }

        public static ApiList<SPList> ToApiList(this IEnumerable<SP.List> source, Guid siteId)
        {
            var apiList = new ApiList<SPList>();
            foreach (SP.List list in source)
            {
                apiList.Add(new SPList(list, siteId));
            }
            return apiList;
        }

        public static T FromXml<T>(this string xml)
        {
            XmlReader stringreader = XmlReader.Create(new StringReader(xml));
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stringreader);
        }

        public static string ToXml<T>(this T obj)
        {
            TextWriter stringWriter = new StringWriter();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stringWriter, obj);
            return stringWriter.ToString();
        }

        public static bool Contains(this string source, string target, StringComparison comp)
        {
            return source.IndexOf(target, comp) >= 0;
        }
    }
}
