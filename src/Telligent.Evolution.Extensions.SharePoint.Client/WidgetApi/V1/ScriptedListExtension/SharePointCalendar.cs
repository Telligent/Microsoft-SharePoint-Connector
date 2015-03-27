using System;
using System.Collections;
using System.Text;
using System.Xml;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.WebServices;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointCalendarExtension : IScriptedContentFragmentExtension
    {
        #region IScriptedContentFragmentExtension

        public string ExtensionName
        {
            get { return "sharepoint_v1_calendar"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointCalendar>(); }
        }

        public string Name
        {
            get { return "SharePoint Calendar functionality (sharepoint_v1_calendar)"; }
        }

        public string Description
        {
            get { return "Allows user to manage Events in SharePoint Calendar lists."; }
        }

        public void Initialize() { }

        #endregion
    }

    public interface ISharePointCalendar
    {
        SPCalendar Get(string url, string listId, IDictionary options);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointCalendar : ISharePointCalendar
    {
        private enum DateRangesOverlap
        {
            Today, Month, Year
        }

        private readonly ICredentialsManager credentials;

        public SharePointCalendar()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal SharePointCalendar(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public SPCalendar Get(string url, string listId,
            [Documentation(Name = "DateRange", Type = typeof(string), Description = "Today, Month, Year"),
            Documentation(Name = "CalendarDate", Type = typeof(DateTime), Description = "Date in UTC format (Now is a default value)"),
            Documentation(Name = "ViewFields", Type = typeof(ArrayList), Description = "An array of field names"),
            Documentation(Name = "DateInUtc", Type = typeof(bool), Description = "False by default"),
            Documentation(Name = "PageSize", Type = typeof(int))]
            IDictionary options)
        {
            var spcalendar = new SPCalendar();
            var auth = credentials.Get(url);

            string webId;
            using (SPContext spcontext = new SPContext(url, auth))
            {
                SP.Web web = spcontext.Web;
                spcontext.Load(web, w => w.Id);
                spcontext.ExecuteQuery();
                webId = web.Id.ToString();
            }

            using (var listService = new ListService(url, auth))
            {
                DateRangesOverlap dateRange = DateRangesOverlap.Month;
                if (options != null && options["DateRange"] != null && Enum.TryParse(options["DateRange"].ToString(), out dateRange)) { }

                var queryDocument = new XmlDocument();

                // CAML Query
                var queryXml = queryDocument.CreateElement("Query");
                queryXml.InnerXml = String.Format(@"
                    <Where>
                        <DateRangesOverlap>
                            <FieldRef Name='EventDate' />
                            <FieldRef Name='EndDate' />
                            <FieldRef Name='RecurrenceID' />
                            <Value Type='DateTime'>
                                <{0}/>
                            </Value>
                        </DateRangesOverlap>
                    </Where>
                    <OrderBy>
                        <FieldRef Name='ID' />
                    </OrderBy>", Enum.GetName(dateRange.GetType(), dateRange));

                // CAML Query options
                bool dateInUtc = false;
                if (options != null && options["DateInUtc"] is bool)
                {
                    dateInUtc = (bool)options["DateInUtc"];
                }

                DateTime calendarDate = dateInUtc ? DateTime.Now : DateTime.UtcNow;
                if (options != null && options["CalendarDate"] is DateTime)
                {
                    calendarDate = (DateTime)options["CalendarDate"];
                }

                var queryOptionsXml = queryDocument.CreateElement("QueryOptions");
                queryOptionsXml.InnerXml = String.Format(@"
                    <IncludeMandatoryColumns>TRUE</IncludeMandatoryColumns>
                    <DateInUtc>{0}</DateInUtc>
                    <ViewAttributes Scope='Recursive' />
                    <RecurrencePatternXMLVersion>v3</RecurrencePatternXMLVersion>
                    <ExpandRecurrence>True</ExpandRecurrence>
                    <CalendarDate>{1}Z</CalendarDate>
                    <RecurrenceOrderBy>TRUE</RecurrenceOrderBy>
                    <ViewAttributes Scope='RecursiveAll'/>
                    <ExpandRecurrence>TRUE</ExpandRecurrence>",
                        dateInUtc.ToString().ToUpper(), calendarDate.ToString("s"));

                // CAML Query View Fields
                var viewFieldsInnerXml = new StringBuilder();
                if (options != null && options["ViewFields"] is ArrayList)
                {
                    var fields = (ArrayList)options["ViewFields"];
                    if (fields != null && fields.Count > 0)
                    {
                        foreach (string fieldName in fields)
                        {
                            viewFieldsInnerXml.AppendFormat("<FieldRef Name='{0}' />", fieldName);
                        }
                    }
                }

                var viewFieldsXml = queryDocument.CreateElement("ViewFields");
                viewFieldsXml.InnerXml = viewFieldsInnerXml.ToString();

                const int defaultPageSize = 10000;
                int pageSize = defaultPageSize;
                if (options != null && options["PageSize"] != null && !String.IsNullOrEmpty(options["PageSize"].ToString()))
                {
                    pageSize = int.Parse(options["PageSize"].ToString());
                }

                var recurrenceEvents = listService.GetListItems(listId, String.Empty, queryXml, viewFieldsXml, pageSize.ToString(), queryOptionsXml, webId);

                var recurrenceEventsDoc = recurrenceEvents.OwnerDocument ?? (XmlDocument)recurrenceEvents;
                var nsmanager = new XmlNamespaceManager(recurrenceEventsDoc.NameTable);
                nsmanager.AddNamespace("ns", "http://schemas.microsoft.com/sharepoint/soap/");
                nsmanager.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
                nsmanager.AddNamespace("z", "#RowsetSchema");

                const string fieldNamePrefix = "ows_";
                foreach (XmlNode itemXml in recurrenceEvents.SelectNodes("//rs:data/z:row", nsmanager))
                {
                    if (itemXml.Attributes[fieldNamePrefix + "UniqueId"] != null)
                    {
                        string[] parsedIds = itemXml.Attributes[fieldNamePrefix + "UniqueId"].Value.Split(new[] { ';' });
                        Guid uniqueId;
                        int counterId;
                        if (parsedIds.Length == 2 && int.TryParse(parsedIds[0], out counterId) && Guid.TryParseExact(parsedIds[1].Replace("#", String.Empty), "B", out uniqueId))
                        {
                            string title = itemXml.Attributes[fieldNamePrefix + "Title"] != null ? itemXml.Attributes[fieldNamePrefix + "Title"].Value : String.Empty;
                            var spevent = new SPEvent(counterId.ToString(), uniqueId.ToString(), title);
                            foreach (XmlAttribute attribute in itemXml.Attributes)
                            {
                                spevent[attribute.Name.Replace(fieldNamePrefix, string.Empty)] = attribute.Value;
                            }
                            spcalendar.Add(spevent);
                        }
                    }
                }

                if (dateRange == DateRangesOverlap.Today)
                {
                    spcalendar.RemoveAll(item =>
                        !item.StartDate.HasValue ||
                        item.StartDate.Value.Day != calendarDate.Day);
                }
            }
            return spcalendar;
        }
    }
}
