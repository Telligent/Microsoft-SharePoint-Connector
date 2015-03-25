using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public abstract class RecurrenceRule
    {
        private readonly RecurrenceType type;
        protected readonly Dictionary<RecurrenceDayOfWeek, string> daysOfWeekPool;

        private RecurrenceRule()
        {
            daysOfWeekPool = new Dictionary<RecurrenceDayOfWeek, string>();
            foreach (RecurrenceDayOfWeek day in Enum.GetValues(typeof(RecurrenceDayOfWeek)))
            {
                daysOfWeekPool.Add(day, day.ToString().ToLowerInvariant());
            }
        }

        public RecurrenceRule(RecurrenceType type)
            : this()
        {
            this.type = type;
        }

        public RecurrenceType Type { get { return type; } }

        public RecurrenceDayOfWeek FirstDayOfWeek { get; set; }

        #region Date Range

        public bool? RepeatForever { get; set; }

        public int? RepeatInstances { get; set; }

        public DateTime? EndBy { get; set; }

        #endregion

        public abstract string ToXml();

        public abstract bool FromXml(XmlNode node);

        protected void AppendFirstDayOfWeek(XmlNode node)
        {
            XmlDocument document = node.OwnerDocument != null ? node.OwnerDocument : (XmlDocument)node;
            XmlElement firstDayXml = document.CreateElement("firstDayOfWeek");
            firstDayXml.InnerText = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek.ToString().Substring(0, 2).ToLowerInvariant();
            node.AppendChild(firstDayXml);
        }

        protected void ParseFirstDayOfWeek(XmlNode node)
        {
            XmlNode firstDayOfWeekNode = node.SelectSingleNode("//firstDayOfWeek");
            if (daysOfWeekPool.ContainsValue(firstDayOfWeekNode.InnerText))
            {
                FirstDayOfWeek = daysOfWeekPool.First(item => item.Value == firstDayOfWeekNode.InnerText).Key;
            }
        }

        protected void AppendDateRange(XmlNode node)
        {
            XmlDocument document = node.OwnerDocument != null ? node.OwnerDocument : (XmlDocument)node;

            if (RepeatInstances.HasValue)
            {
                XmlElement repeatInstancesXml = document.CreateElement("repeatInstances");
                repeatInstancesXml.InnerText = RepeatInstances.Value.ToString();
                node.AppendChild(repeatInstancesXml);
                return;
            }

            if (EndBy.HasValue)
            {
                XmlElement windowEndXml = document.CreateElement("windowEnd");
                windowEndXml.InnerText = String.Format("{0}Z", EndBy.Value.ToUniversalTime().ToString("s"));
                node.AppendChild(windowEndXml);
                return;
            }

            XmlElement repeatForeverXml = document.CreateElement("repeatForever");
            repeatForeverXml.InnerText = (RepeatForever ?? false).ToString().ToUpperInvariant();
            node.AppendChild(repeatForeverXml);
        }

        protected void ParseDateRange(XmlNode node)
        {
            RepeatForever = null;
            XmlNode repeatForeverNode = node.SelectSingleNode("//repeatForever");
            if (repeatForeverNode != null)
            {
                bool repeatForever;
                if (bool.TryParse(repeatForeverNode.InnerText, out repeatForever))
                {
                    RepeatForever = repeatForever;
                }
            }

            RepeatInstances = null;
            XmlNode repeatInstancesNode = node.SelectSingleNode("//repeatInstances");
            if (repeatInstancesNode != null)
            {
                int repeatInstances;
                if (int.TryParse(repeatInstancesNode.InnerText, out repeatInstances))
                {
                    RepeatInstances = repeatInstances;
                }
            }

            EndBy = null;
            XmlNode windowEndNode = node.SelectSingleNode("//windowEnd");
            if (windowEndNode != null)
            {
                DateTime windowEnd;
                if (DateTime.TryParse(windowEndNode.InnerText, out windowEnd))
                {
                    EndBy = windowEnd;
                }
            }
        }
    }
}
