using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class Author : IApiEntity
    {
        public Author(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public Author(int lookupId)
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
                return new Expression<Func<ListItem, object>>[]
                    {
                        item => item.Id,
                        item => item["UniqueId"],
                        item => item["Name"],
                        item => item["Title"],
                        item => item["Picture"],
                        item => item["EMail"],
                        item => item["SipAddress"]
                    };
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

        private const string defaultAvatarUrl = "/utility/anonymous.gif";
        private string avatarUrl;
        /// <summary>
        /// Avatar Url
        /// </summary>
        public string AvatarUrl
        {
            get
            {
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    if (!string.IsNullOrEmpty(Email))
                    {
                        var user = TEApi.Users.Get(new UsersGetOptions { Email = Email });
                        avatarUrl = (user != null) ? user.AvatarUrl : defaultAvatarUrl;
                    }
                    else
                    {
                        return defaultAvatarUrl;
                    }
                }
                return avatarUrl;
            }
            private set
            {
                avatarUrl = value;
            }
        }

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
            Id = userItem["UniqueId"].ToString();
            DisplayName = userItem["Title"].ToString();
            Name = userItem["Name"] != null ? userItem["Name"].ToString() : DisplayName;

            string sipAddress = userItem["SipAddress"] != null ? userItem["SipAddress"].ToString() : string.Empty;
            if (String.IsNullOrEmpty(sipAddress))
            {
                sipAddress = userItem["EMail"] != null ? userItem["EMail"].ToString() : string.Empty;
            }
            Email = sipAddress;

            var pictureVal = userItem["Picture"] as FieldUrlValue;
            var picture = pictureVal != null ? pictureVal.Url.ToString(CultureInfo.InvariantCulture) : string.Empty;

            if (!string.IsNullOrEmpty(picture))
            {
                AvatarUrl = picture;
            }
        }

        #region IApiEntity

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        #endregion
    }
}
