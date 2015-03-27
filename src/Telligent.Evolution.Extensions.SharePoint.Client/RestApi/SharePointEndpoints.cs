using System;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Extensibility.Version1;
using RestApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.RestApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest
{
    public class SharePointEndpoints : IRestEndpoints, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string InvalidListId = "restapi_invalid_listid";
            public const string InvalidViewId = "restapi_invalid_viewid";
            public const string UnknownError = "restapi_unknown_error";
            public const string UrlCannotBeEmpty = "restapi_url_cannot_be_empty";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string Name
        {
            get { return "SharePoint Integration REST Routes"; }
        }

        public string Description
        {
            get { return "Implements the REST routes for SharePoint Integration."; }
        }

        public void Initialize() { }

        #endregion

        #region IRestEndpoints Members

        public void Register(IRestEndpointController controller)
        {
            controller.Add(2, "sharepoint/lists", new { }, new { }, HttpMethod.Get, request => RestApi.List.List(new SPListCollectionRequest(request)));
            controller.Add(2, "sharepoint/lists/{listId}", new { }, new { }, HttpMethod.Get, request => RestApi.List.Get(new SPListItemRequest(request)));
            controller.Add(2, "sharepoint/lists/{listId}/views", new { }, new { }, HttpMethod.Get, request => RestApi.View.List(new SPViewCollectionRequest(request)));
            controller.Add(2, "sharepoint/lists/{listId}/views/{viewId}", new { }, new { }, HttpMethod.Get, request => RestApi.View.Get(new SPViewItemRequest(request)));

            controller.Add(2, "sharepoint/users", new { }, new { }, HttpMethod.Get, request => RestApi.UserOrGroup.List(new SPUserOrGroupRequest(request), onlyGroups: false, onlyUsers: true));
            controller.Add(2, "sharepoint/groups", new { }, new { }, HttpMethod.Get, request => RestApi.UserOrGroup.List(new SPUserOrGroupRequest(request), onlyGroups: true));
            controller.Add(2, "sharepoint/usersandgroups", new { }, new { }, HttpMethod.Get, request => RestApi.UserOrGroup.List(new SPUserOrGroupRequest(request)));
        }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.InvalidListId, "The List Id is invalid.");
                t.Set(Translations.InvalidViewId, "The View Id is invalid.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");
                t.Set(Translations.UrlCannotBeEmpty, "SharePoint web site url cannot be empty.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        internal string Translate(string key, params object[] args)
        {
            return String.Format(translationController.GetLanguageResourceValue(key), args);
        }
    }
}
