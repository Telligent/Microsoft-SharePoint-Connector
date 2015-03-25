using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPUser : IApiEntity
    {
        public SPUser(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public SPUser(ListItem userItem)
        {
            Initialize(userItem);
        }

        public SPUser(int lookupId)
        {
            LookupId = lookupId;
        }

        /// <summary>
        /// Query for a SP COM request
        /// </summary>
        public static Expression<Func<ListItem, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<ListItem, object>>[]{
                    item => item.Id,
                    item => item["UniqueId"],
                    item => item["Name"],
                    item => item["Title"],
                    item => item["Picture"],
                    item => item["EMail"],
                    item => item["SipAddress"]};
            }
        }

        /// <summary>
        /// Counter Id
        /// </summary>
        public int LookupId { get; private set; }

        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Account name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Avatar Url
        /// </summary>
        public string AvatarUrl { get; private set; }

        /// <summary>
        /// Email or sipAddress for Telligent connection
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// The method populate properties using values from SPListItem
        /// </summary>
        /// <param name="userItem">SharePoint List Item</param>
        public void Initialize(ListItem userItem)
        {
            LookupId = userItem.Id;

            Id = userItem["UniqueId"].ToString();
            Name = userItem["Name"] != null ? userItem["Name"].ToString() : string.Empty;
            DisplayName = userItem["Title"] != null ? userItem["Title"].ToString() : Name;

            AvatarUrl = string.Empty;
            var pictureVal = userItem["Picture"] as FieldUrlValue;
            if (pictureVal != null)
            {
                AvatarUrl = pictureVal.Url;
            }

            string sipAddress = userItem["SipAddress"] != null ? userItem["SipAddress"].ToString() : string.Empty;
            if (String.IsNullOrEmpty(sipAddress))
            {
                sipAddress = userItem["EMail"] != null ? userItem["EMail"].ToString() : string.Empty;
            }
            Email = sipAddress;
        }

        #region IApiEntity Members

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }

        #endregion
    }
}
