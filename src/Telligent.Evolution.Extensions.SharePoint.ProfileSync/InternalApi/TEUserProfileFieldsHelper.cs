using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using V1 = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    internal static class TEUserProfileFieldsHelper
    {
        private static readonly Dictionary<string, string> fieldsAvailableForSync = new Dictionary<string, string> 
        {
            {"AvatarUrl", "Avatar Url"},
            {"Bio", "Bio"},
            {"DisplayName", "Display Name"}, 
            {"Location", "Location"},
            {"PublicEmail", "Public Email"},
            {"Signature", "Signature"}, 
            {"Username", "User Name"}
        };

        internal static List<ProfileField> GetFields()
        {
            var fields = new List<ProfileField>();

            const int pageSize = 100;
            int pageIndex = 0;

            V1.PagedList<V1.UserProfileField> fieldsPagedList = null;
            do
            {
                fieldsPagedList = PublicApi.UserProfileFields.List(new UserProfileFieldsListOptions { PageIndex = pageIndex, PageSize = pageSize });
                fields.AddRange(fieldsPagedList.Select(f => new ProfileField(f.Name.Replace(" ", String.Empty), f.Title, true)));
                pageIndex++;
            }
            while (fieldsPagedList != null && fieldsPagedList.TotalCount > pageSize * pageIndex);

            foreach (var fieldName in fieldsAvailableForSync.Keys)
            {
                fields.Add(new ProfileField(fieldName, fieldsAvailableForSync[fieldName], true));
            }
            return fields;
        }
    }
}
