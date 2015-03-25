using System;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class YearlyRule : RecurrenceRule
    {
        public YearlyRule()
            : base(RecurrenceType.Yearly)
        {
        }

        public bool YearlyByDay { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Frequency { get; set; }

        public RecurrenceDayOfWeek WeekDay { get; set; }

        public RecurrenceOrder WeekDayOrder { get; set; }

        public RecurrenceDayOfWeek? SetWeekDay(string weekDay)
        {
            RecurrenceDayOfWeek recurrenceDayOfWeek;
            if (Enum.TryParse(weekDay, out recurrenceDayOfWeek))
            {
                WeekDay = recurrenceDayOfWeek;
                return recurrenceDayOfWeek;
            }
            return null;
        }

        public RecurrenceOrder? SetWeekDayOrder(string weekDayOrder)
        {
            RecurrenceOrder recurrenceOrder;
            if (Enum.TryParse(weekDayOrder, out recurrenceOrder))
            {
                WeekDayOrder = recurrenceOrder;
                return recurrenceOrder;
            }
            return null;
        }

        public override string ToXml()
        {
            XmlDocument yearlyRuleDocument = new XmlDocument();

            XmlElement recurrenceXml = yearlyRuleDocument.CreateElement("recurrence");
            yearlyRuleDocument.AppendChild(recurrenceXml);

            XmlElement ruleXml = yearlyRuleDocument.CreateElement("rule");
            recurrenceXml.AppendChild(ruleXml);

            AppendFirstDayOfWeek(ruleXml);

            XmlElement repeatXml = yearlyRuleDocument.CreateElement("repeat");
            ruleXml.AppendChild(repeatXml);

            if (YearlyByDay)
            {
                XmlElement yearlyXml = yearlyRuleDocument.CreateElement("yearlyByDay");
                yearlyXml.SetAttribute(daysOfWeekPool[WeekDay], "TRUE");
                yearlyXml.SetAttribute("weekdayOfMonth", Enum.GetName(typeof(RecurrenceOrder), WeekDayOrder));
                yearlyXml.SetAttribute("month", Month.ToString());
                yearlyXml.SetAttribute("yearFrequency", this.Frequency.ToString());
                repeatXml.AppendChild(yearlyXml);
            }
            else
            {
                XmlElement yearlyXml = yearlyRuleDocument.CreateElement("yearly");
                yearlyXml.SetAttribute("day", Day.ToString());
                yearlyXml.SetAttribute("month", Month.ToString());
                yearlyXml.SetAttribute("yearFrequency", this.Frequency.ToString());
                repeatXml.AppendChild(yearlyXml);
            }

            AppendDateRange(ruleXml);

            return yearlyRuleDocument.InnerXml;
        }

        public override bool FromXml(XmlNode node)
        {
            ParseDateRange(node);
            ParseFirstDayOfWeek(node);

            Frequency = default(int);
            XmlNode yearlyNode = node.SelectSingleNode("//yearly");
            if (yearlyNode != null && yearlyNode.Attributes["yearFrequency"] != null)
            {
                int yearFrequency;
                if (int.TryParse(yearlyNode.Attributes["yearFrequency"].Value, out yearFrequency))
                {
                    Frequency = yearFrequency;
                }

                int day;
                if (yearlyNode.Attributes["day"] != null && int.TryParse(yearlyNode.Attributes["day"].Value, out day))
                {
                    this.Day = day;
                }

                int month;
                if (yearlyNode.Attributes["month"] != null && int.TryParse(yearlyNode.Attributes["month"].Value, out month))
                {
                    this.Month = month;
                }

                this.YearlyByDay = false;
                return true;
            }

            yearlyNode = node.SelectSingleNode("//yearlyByDay");
            if (yearlyNode != null && yearlyNode.Attributes["yearFrequency"] != null)
            {
                int yearFrequency;
                if (int.TryParse(yearlyNode.Attributes["yearFrequency"].Value, out yearFrequency))
                {
                    Frequency = yearFrequency;
                }

                foreach (string day in daysOfWeekPool.Values)
                {
                    bool enabled = false;
                    if (yearlyNode.Attributes[day] != null && bool.TryParse(yearlyNode.Attributes[day].Value, out enabled) && enabled)
                    {
                        WeekDay = daysOfWeekPool.First(item => item.Value == day).Key;
                        break;
                    }
                }

                RecurrenceOrder number;
                if (yearlyNode.Attributes["weekdayOfMonth"] != null && Enum.TryParse(yearlyNode.Attributes["weekdayOfMonth"].Value, out number))
                {
                    WeekDayOrder = number;
                }

                int month;
                if (yearlyNode.Attributes["month"] != null && int.TryParse(yearlyNode.Attributes["month"].Value, out month))
                {
                    this.Month = month;
                }

                this.YearlyByDay = true;
                return true;
            }

            return false;
        }
    }
}
