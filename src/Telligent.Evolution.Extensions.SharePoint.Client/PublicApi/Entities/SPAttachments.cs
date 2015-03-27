using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Entities
{
    public class SPAttachment : ApiEntity
    {
        public SPAttachment(AdditionalInfo additionalInfo) : base(additionalInfo) { }
        public SPAttachment(IList<Warning> warnings, IList<Error> errors) : base(warnings, errors) { }
        public SPAttachment(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }

        public string Name { get; private set; }
        public Uri Uri { get; private set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public SPUserPrincipal CreatedBy { get; set; }
        public SPUserPrincipal ModifiedBy { get; set; }
    }
}
