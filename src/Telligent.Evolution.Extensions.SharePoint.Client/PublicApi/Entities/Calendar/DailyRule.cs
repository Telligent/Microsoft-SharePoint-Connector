using System;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class DailyRule : RecurrenceRule
    {
        public DailyRule()
            : base(RecurrenceType.Daily)
        { }

        public RecurrenceDayOfWeek? WeekDay { get; set; }

        public int Frequency { get; set; }

        public void SetWeekDay(string weekDay)
        {
            RecurrenceDayOfWeek recurrenceDayOfWeek;
            if (Enum.TryParse(weekDay, out recurrenceDayOfWeek))
            {
                WeekDay = recurrenceDayOfWeek;
            }
        }

        public override string ToXml()
        {
            XmlDocument dailyRuleDocument = new XmlDocument();

            XmlElement recurrenceXml = dailyRuleDocument.CreateElement("recurrence");
            dailyRuleDocument.AppendChild(recurrenceXml);

            XmlElement ruleXml = dailyRuleDocument.CreateElement("rule");
            recurrenceXml.AppendChild(ruleXml);

            AppendFirstDayOfWeek(ruleXml);

            XmlElement repeatXml = dailyRuleDocument.CreateElement("repeat");
            ruleXml.AppendChild(repeatXml);

            XmlElement dailyXml = dailyRuleDocument.CreateElement("daily");
            if (WeekDay.HasValue)
            {
                dailyXml.SetAttribute(daysOfWeekPool[WeekDay.Value], "TRUE");
            }
            else
            {
                dailyXml.SetAttribute("dayFrequency", this.Frequency.ToString());
            }
            repeatXml.AppendChild(dailyXml);

            AppendDateRange(ruleXml);

            return dailyRuleDocument.InnerXml;
        }

        public override bool FromXml(XmlNode node)
        {
            ParseDateRange(node);
            ParseFirstDayOfWeek(node);

            Frequency = default(int);
            XmlNode dailyNode = node.SelectSingleNode("//daily");
            if (dailyNode != null)
            {
                if (dailyNode.Attributes["dayFrequency"] != null)
                {
                    int dayFrequency;
                    if (int.TryParse(dailyNode.Attributes["dayFrequency"].Value, out dayFrequency))
                    {
                        Frequency = dayFrequency;
                    }
                    return true;
                }

                foreach (string day in daysOfWeekPool.Values)
                {
                    bool enabled = false;
                    if (dailyNode.Attributes[day] != null && bool.TryParse(dailyNode.Attributes[day].Value, out enabled) && enabled)
                    {
                        WeekDay = daysOfWeekPool.First(item => item.Value == day).Key;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
