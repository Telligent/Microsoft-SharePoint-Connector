using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    class RecurrenceRulePool
    {
        private readonly List<RecurrenceRule> pool;

        public RecurrenceRulePool()
        {
            pool = new List<RecurrenceRule>
            {
                new DailyRule(),
                new WeeklyRule(),
                new MonthlyRule(),
                new YearlyRule()
            };
        }

        public RecurrenceRule FromXml(XmlNode node)
        {
            foreach (RecurrenceRule rule in pool)
            {
                if (rule.FromXml(node))
                {
                    return rule;
                }
            }
            return null;
        }

        public RecurrenceRule ByType(RecurrenceType type)
        {
            return pool.FirstOrDefault(item => item.Type == type);
        }
    }
}
