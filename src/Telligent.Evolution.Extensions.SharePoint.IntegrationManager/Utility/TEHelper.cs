using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Version1;
using API = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public static class TEHelper
    {
        public static string GetGroupName(int id)
        {
            API.Group group = PublicApi.Groups.Get(new GroupsGetOptions
            {
                Id = id
            });
            return group.Name;
        }

        public static API.Group GetGroupById(int? id)
        {
            API.Group group = PublicApi.Groups.Get(new GroupsGetOptions
            {
                Id = id
            });
            return group;
        }

        public static IList<API.Group> GetChildGroups(API.Group group)
        {
            return GetChildGroups(group.Id);
        }

        public static IList<API.Group> GetChildGroups(int? id)
        {
            API.PagedList<API.Group> groups = PublicApi.Groups.List(new GroupsListOptions
            {
                ParentGroupId = id
            });
            return groups;
        }
    }
}
