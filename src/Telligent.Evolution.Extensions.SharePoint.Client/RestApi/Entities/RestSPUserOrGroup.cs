using System;
using System.Globalization;
using System.Linq;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rest.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities
{
    public class RestSPUserOrGroup : RestEntity<int>
    {
        public const string PersonContentType = "0x010A";
        public const string SharePointGroupContentType = "0x010B";
        public const string DomainGroupPersonContentType = "0x010C";
        
        public static readonly string[] ViewFields = new[] { "Id", "UniqueId", "Title", "Name", "EMail", "Picture", "ContentTypeId" };

        public Guid UniqueId { get; set; }
        public string Title { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string Email { get; set; }
        public bool IsGroup { get; set; }

        public static RestSPUserOrGroup Get(ListItem spuserOrGroup)
        {
            var restUserOrGroup = new RestSPUserOrGroup
            {
                Id = spuserOrGroup.Id,
                UniqueId = Guid.Parse(spuserOrGroup["UniqueId"].ToString()),
                Title = spuserOrGroup["Title"] != null ? spuserOrGroup["Title"].ToString() : string.Empty,
                Name = spuserOrGroup["Name"] != null ? spuserOrGroup["Name"].ToString() : string.Empty,
                AvatarUrl = GetAvatarUrl(spuserOrGroup),
                IsGroup = spuserOrGroup["ContentTypeId"] != null && !spuserOrGroup["ContentTypeId"].ToString().StartsWith(PersonContentType)
            };

            if (!restUserOrGroup.IsGroup)
            {
                restUserOrGroup.Email = spuserOrGroup["EMail"] != null ? spuserOrGroup["EMail"].ToString() : string.Empty;
            }

            restUserOrGroup.DisplayName = string.IsNullOrEmpty(restUserOrGroup.Title) ? restUserOrGroup.Name.Split('|').Last() : restUserOrGroup.Title;

            if (!string.IsNullOrEmpty(restUserOrGroup.Email))
            {
                var user = PublicApi.Users.Get(new UsersGetOptions { Email = restUserOrGroup.Email });
                if (user != null && !string.IsNullOrEmpty(user.DisplayName))
                {
                    restUserOrGroup.DisplayName = user.DisplayName;
                }
            }

            return restUserOrGroup;
        }

        private static string GetAvatarUrl(ListItem userItem)
        {
            var pictureVal = userItem["Picture"] as FieldUrlValue;
            if (pictureVal != null)
            {
                return pictureVal.Url.ToString(CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }
    }
}
