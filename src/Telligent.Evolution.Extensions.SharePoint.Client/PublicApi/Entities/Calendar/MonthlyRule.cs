using System;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class MonthlyRule : RecurrenceRule
    {
        public MonthlyRule()
            : base(RecurrenceType.Monthly)
        {
            Frequency = 1;
        }

        public bool MonthlyByDay { get; set; }

        public int Day { get; set; }

        public RecurrenceDayOfWeek WeekDay { get; set; }

        public RecurrenceOrder WeekDayOrder { get; set; }

        public int Frequency { get; set; }

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
            XmlDocument monthlyRuleDocument = new XmlDocument();

            XmlElement recurrenceXml = monthlyRuleDocument.CreateElement("recurrence");
            monthlyRuleDocument.AppendChild(recurrenceXml);

            XmlElement ruleXml = monthlyRuleDocument.CreateElement("rule");
            recurrenceXml.AppendChild(ruleXml);

            AppendFirstDayOfWeek(ruleXml);

            XmlElement repeatXml = monthlyRuleDocument.CreateElement("repeat");
            ruleXml.AppendChild(repeatXml);

            if (MonthlyByDay)
            {
                XmlElement monthlyXml = monthlyRuleDocument.CreateElement("monthlyByDay");
                monthlyXml.SetAttribute(daysOfWeekPool[WeekDay], "TRUE");
                monthlyXml.SetAttribute("weekdayOfMonth", Enum.GetName(typeof(RecurrenceOrder), WeekDayOrder));
                monthlyXml.SetAttribute("monthFrequency", this.Frequency.ToString());
                repeatXml.AppendChild(monthlyXml);
            }
            else
            {
                XmlElement monthlyXml = monthlyRuleDocument.CreateElement("monthly");
                monthlyXml.SetAttribute("day", Day.ToString());
                monthlyXml.SetAttribute("monthFrequency", this.Frequency.ToString());
                repeatXml.AppendChild(monthlyXml);
            }

            AppendDateRange(ruleXml);

            return monthlyRuleDocument.InnerXml;
        }

        public override bool FromXml(XmlNode node)
        {
            ParseDateRange(node);
            ParseFirstDayOfWeek(node);

            Frequency = default(int);
            XmlNode monthlyNode = node.SelectSingleNode("//monthly");
            if (monthlyNode != null && monthlyNode.Attributes["monthFrequency"] != null)
            {
                int monthFrequency;
                if (int.TryParse(monthlyNode.Attributes["monthFrequency"].Value, out monthFrequency))
                {
                    Frequency = monthFrequency;
                }

                int day;
                if (monthlyNode.Attributes["day"] != null && int.TryParse(monthlyNode.Attributes["day"].Value, out day))
                {
                    this.Day = day;
                }
                this.MonthlyByDay = false;
                return true;
            }

            monthlyNode = node.SelectSingleNode("//monthlyByDay");
            if (monthlyNode != null && monthlyNode.Attributes["monthFrequency"] != null)
            {
                int monthFrequency;
                if (int.TryParse(monthlyNode.Attributes["monthFrequency"].Value, out monthFrequency))
                {
                    Frequency = monthFrequency;
                }

                foreach (string day in daysOfWeekPool.Values)
                {
                    bool enabled = false;
                    if (monthlyNode.Attributes[day] != null && bool.TryParse(monthlyNode.Attributes[day].Value, out enabled) && enabled)
                    {
                        WeekDay = daysOfWeekPool.First(item => item.Value == day).Key;
                        break;
                    }
                }

                RecurrenceOrder number;
                if (monthlyNode.Attributes["weekdayOfMonth"] != null && Enum.TryParse(monthlyNode.Attributes["weekdayOfMonth"].Value, out number))
                {
                    WeekDayOrder = number;
                }

                this.MonthlyByDay = true;
                return true;
            }
            return false;
        }
    }
}