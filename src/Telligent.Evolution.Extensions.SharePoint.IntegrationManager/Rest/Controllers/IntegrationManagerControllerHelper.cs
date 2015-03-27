using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Entities;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Resources;
using Telligent.Evolution.Rest.Infrastructure.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Controllers
{
    public static class IntegrationManagerControllerHelper
    {
        public static IRestResponse Get(IntegrationManagerRequest request)
        {
            var response = new DefaultRestResponse
                {
                    Name = "IntegrationManager"
                };

            try
            {
                RestIntegrationManager manager = null;
                bool hasManagerId = !String.IsNullOrEmpty(request.ManagerId);
                bool hasGroupId = request.GroupId.HasValue;
                if (hasManagerId)
                {
                    manager = new RestIntegrationManager(IntegrationManagerPlugin.GetAllProviders().FirstOrDefault(m => m.Id == request.ManagerId));
                }
                else if (hasGroupId)
                {
                    manager = new RestIntegrationManager(IntegrationManagerPlugin.GetAllProviders().FirstOrDefault(m => m.TEGroupId == request.GroupId.Value));
                }
                response.Data = manager;
            }
            catch (Exception ex)
            {
                response.Errors = new[] { ex.Message };
            }
            return response;
        }

        public static IRestResponse List(IntegrationManagerListRequest request)
        {
            var response = new DefaultRestResponse
            {
                Name = "IntegrationManagerList"
            };

            try
            {
                List<IntegrationProvider> managerList = null;

                Func<IntegrationProvider, bool> filter = (m => true);
                bool hasSiteNameFilter = !String.IsNullOrEmpty(request.SiteNameFilter);
                bool hasGroupNameFilter = !String.IsNullOrEmpty(request.GroupNameFilter);
                if (hasSiteNameFilter && hasGroupNameFilter)
                {
                    filter = (m => m.SPSiteName.Contains(request.SiteNameFilter, StringComparison.OrdinalIgnoreCase) || m.TEGroupName.Contains(request.GroupNameFilter, StringComparison.OrdinalIgnoreCase));
                }
                else if (hasSiteNameFilter)
                {
                    filter = (m => m.SPSiteName.Contains(request.SiteNameFilter, StringComparison.OrdinalIgnoreCase));
                }
                else if (hasGroupNameFilter)
                {
                    filter = (m => m.TEGroupName.Contains(request.GroupNameFilter, StringComparison.OrdinalIgnoreCase));
                }

                managerList = IntegrationManagerPlugin.GetAllProviders().Where(filter).ToList();
                response.Data = new IntegrationManagerListData(managerList.Skip(request.PageIndex).Take(request.PageSize).Select(m => new RestIntegrationManager(m)), managerList.Count);
            }
            catch (Exception ex)
            {
                response.Errors = new[] { ex.Message };
            }
            return response;
        }

        public static bool Contains(this string source, string target, StringComparison comp)
        {
            return source.IndexOf(target, comp) >= 0;
        }
    }
}
