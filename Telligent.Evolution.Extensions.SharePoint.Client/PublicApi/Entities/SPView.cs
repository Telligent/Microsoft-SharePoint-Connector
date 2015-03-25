using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPView : IApiEntity
    {
        public static Expression<Func<View, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<View, object>>[]{
                    view => view,
                    view => view.Id,
                    view => view.Title,
                    view => view.Hidden,
                    view => view.ViewQuery,
                    view => view.HtmlSchemaXml};
            }
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public List<string> Fields { get; private set; }
        public Dictionary<string, string> Columns { get; private set; }
        public string Query { get; private set; }

        private View _spview;

        public SPView(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public SPView(View spview, Dictionary<string, string> columns)
        {
            _spview = spview;
            this.Id = spview.Id.ToString();
            this.Name = spview.Title;
            this.Fields = ReadFieldNamesFromViewXml(spview.HtmlSchemaXml);
            this.Columns = columns;
            this.Query = spview.ViewQuery;
        }

        private static List<string> ReadFieldNamesFromViewXml(string viewXml)
        {
            List<string> fields = new List<string>();
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xmlReader = XmlReader.Create(
                new StringReader(viewXml), readerSettings);
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "FieldRef")
                {
                    while (xmlReader.MoveToNextAttribute())
                    {
                        if (xmlReader.Name == "Name")
                        {
                            string fieldName = xmlReader.Value;
                            if (fieldName != "ID" && !fields.Contains(fieldName))
                            {
                                fields.Add(fieldName);
                            }
                        }
                    }
                }
            }
            return fields;
        }

        public string this[string fieldName]
        {
            get
            {
                if (Columns.ContainsKey(fieldName))
                {
                    return Columns[fieldName];
                }
                return fieldName;
            }
        }

        #region IApiEntity Members

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }

        #endregion

        #region Overriden
        public override bool Equals(object obj)
        {
            SPView target = obj as SPView;
            if (target != null)
            {
                foreach (var property in typeof(SPView).GetProperties(System.Reflection.BindingFlags.CreateInstance))
                {
                    if (!property.GetValue(this, null).Equals(property.GetValue(target, null)))
                        return false;
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}
