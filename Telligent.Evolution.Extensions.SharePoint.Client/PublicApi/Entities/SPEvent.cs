using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPEvent : IApiEntity
    {
        private readonly Dictionary<string, string> fields;

        private SPEvent()
        {
            fields = new Dictionary<string, string>();
        }

        public SPEvent(string id, string uniqueId, string title)
            : this()
        {
            Id = id;
            UniqueId = uniqueId;
            Title = title;
        }

        public SPEvent(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public string Id { get; private set; }

        public string UniqueId { get; private set; }

        public string Title { get; private set; }

        public DateTime? StartDate
        {
            get
            {
                if (fields.ContainsKey("EventDate"))
                {
                    return DateTime.Parse(fields["EventDate"]);
                }
                return null;
            }
        }

        public DateTime? EndDate
        {
            get
            {
                if (fields.ContainsKey("EndDate"))
                {
                    return DateTime.Parse(fields["EndDate"]);
                }
                return null;
            }
        }

        public bool AllDayEvent
        {
            get
            {
                return fields.ContainsKey("fAllDayEvent") && fields["fAllDayEvent"] == "1";
            }
        }

        public string this[string fieldName]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                {
                    return fields[fieldName];
                }
                return null;
            }
            set
            {
                if (!fields.ContainsKey(fieldName))
                {
                    fields.Add(fieldName, value);
                }
                else
                {
                    fields[fieldName] = value;
                }
            }
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
