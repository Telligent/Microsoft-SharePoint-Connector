using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPCalendar : List<SPEvent>, IApiEntity
    {
        public SPCalendar() { }

        public SPCalendar(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public IEnumerable<SPEvent> Where(int year, int month)
        {
            return (from spevent in this
                    where spevent.StartDate.HasValue 
                       && spevent.StartDate.Value.Year  == year 
                       && spevent.StartDate.Value.Month == month
                    orderby spevent.StartDate.Value.TimeOfDay
                    select spevent).ToList();
        }

        public IEnumerable<SPEvent> Where(int year, int month, int day)
        {
            return (from spevent in this
                    where spevent.StartDate.HasValue
                       && spevent.StartDate.Value.Year  == year
                       && spevent.StartDate.Value.Month == month
                       && spevent.StartDate.Value.Day   == day
                    orderby spevent.StartDate.Value.TimeOfDay
                    orderby spevent.EndDate.Value - spevent.StartDate.Value
                    select spevent).ToList();
        }

        public IEnumerable<SPEvent> AllDayEvents()
        {
            return (from spevent in this
                    where spevent.AllDayEvent
                    orderby spevent.Id
                    select spevent).ToList();
        }

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }
    }
}
