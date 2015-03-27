using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class WeeklyRule : RecurrenceRule
    {
        public WeeklyRule()
            : base(RecurrenceType.Weekly)
        {
            DaysOfWeek = new List<RecurrenceDayOfWeek>();
        }

        public List<RecurrenceDayOfWeek> DaysOfWeek { get; set; }

        public int Frequency { get; set; }

        public RecurrenceDayOfWeek? Contains(string dayOfWeek)
        {
            RecurrenceDayOfWeek day;
            if (Enum.TryParse(dayOfWeek, out day) && DaysOfWeek.Contains(day))
            {
                return day;
            }
            return null;
        }

        public RecurrenceDayOfWeek? AddDayOfWeek(string dayOfWeek)
        {
            RecurrenceDayOfWeek day;
            if (Enum.TryParse(dayOfWeek, out day) && !DaysOfWeek.Contains(day))
            {
                DaysOfWeek.Add(day);
            }
            else
            {
                return null;
            }
            return day;
        }

        public RecurrenceDayOfWeek RemoveDayOfWeek(string dayOfWeek)
        {
            RecurrenceDayOfWeek day;
            if (Enum.TryParse(dayOfWeek, out day) && DaysOfWeek.Contains(day))
            {
                DaysOfWeek.RemoveAll(item => item == day);
            }
            return day;
        }

        public override string ToXml()
        {
            XmlDocument weeklyRuleDocument = new XmlDocument();

            XmlElement recurrenceXml = weeklyRuleDocument.CreateElement("recurrence");
            weeklyRuleDocument.AppendChild(recurrenceXml);

            XmlElement ruleXml = weeklyRuleDocument.CreateElement("rule");
            recurrenceXml.AppendChild(ruleXml);

            AppendFirstDayOfWeek(ruleXml);

            XmlElement repeatXml = weeklyRuleDocument.CreateElement("repeat");
            ruleXml.AppendChild(repeatXml);

            XmlElement weeklyXml = weeklyRuleDocument.CreateElement("weekly");
            weeklyXml.SetAttribute("weekFrequency", this.Frequency.ToString());
            foreach (RecurrenceDayOfWeek day in DaysOfWeek)
            {
                string attrName = daysOfWeekPool[day];
                if (!weeklyXml.HasAttribute(attrName))
                {
                    weeklyXml.SetAttribute(attrName, "TRUE");
                }
            }
            repeatXml.AppendChild(weeklyXml);

            AppendDateRange(ruleXml);

            return weeklyRuleDocument.InnerXml;
        }

        public override bool FromXml(XmlNode node)
        {
            ParseDateRange(node);
            ParseFirstDayOfWeek(node);

            Frequency = default(int);
            DaysOfWeek = new List<RecurrenceDayOfWeek>();
            XmlNode weeklyNode = node.SelectSingleNode("//weekly");
            if (weeklyNode != null && weeklyNode.Attributes["weekFrequency"] != null)
            {
                int weekFrequency;
                if (int.TryParse(weeklyNode.Attributes["weekFrequency"].Value, out weekFrequency))
                {
                    Frequency = weekFrequency;
                }

                foreach (string day in daysOfWeekPool.Values)
                {
                    bool enabled = false;
                    if (weeklyNode.Attributes[day] != null && bool.TryParse(weeklyNode.Attributes[day].Value, out enabled) && enabled)
                    {
                        DaysOfWeek.Add(daysOfWeekPool.First(item => item.Value == day).Key);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
