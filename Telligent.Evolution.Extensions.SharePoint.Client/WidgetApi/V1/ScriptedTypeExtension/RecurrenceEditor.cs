using System;
using System.Xml;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class RecurrenceEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_recurrence"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IRecurrenceEditor>(); }
        }

        public string Name
        {
            get { return "SharePoint Recurrence Events functionality (sharepoint_v1_recurrence)"; }
        }

        public string Description
        {
            get { return "Allows user to manage recurrence events in SharePoint Calendar lists."; }
        }

        public void Initialize() { }
    }

    public interface IRecurrenceEditor
    {
        RecurrenceRule Parse(string recurrenceData);

        RecurrenceRule Create(string typeOfRule);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class RecurrenceEditor : IRecurrenceEditor
    {
        [Documentation(Name = "Recurrence Rule", Type = typeof(string), Description = "Daily, Weekly, Monthly or Yearly")]
        public RecurrenceRule Parse(string recurrenceData)
        {
            var recurrenceXml = new XmlDocument();
            try
            {
                recurrenceXml.LoadXml(recurrenceData);
            }
            catch (Exception ex)
            {
                SPLog.SiteSettingsInvalidXML(ex, String.Format("An exception of type {0} occurred while parsing recurrenceData in XML format for a recurrence rule. The exception message is: {1}",ex.GetType().Name, ex.Message));
            }
            var recurrenceRules = new RecurrenceRulePool();
            return recurrenceRules.FromXml(recurrenceXml);
        }

        public RecurrenceRule Create([Documentation(Name = "typeOfRule", Type = typeof(string), Description = "Daily, Weekly, Monthly or Yearly")]
            string typeOfRule)
        {
            RecurrenceType rule;
            if (Enum.TryParse(typeOfRule, out rule))
            {
                var recurrenceRules = new RecurrenceRulePool();
                return recurrenceRules.ByType(rule);
            }
            return null;
        }
    }
}
