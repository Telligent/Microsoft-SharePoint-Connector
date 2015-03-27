using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using ApiUser = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync
{
    public static class ExecuteProfileSyncHelper
    {
        [Flags]
        public enum MergeResult
        {
            None = 0,
            ExternalUpdated = 1,
            InternalUpdated = 2
        }

        private const string AvatarUrlField = "AvatarUrl";

        internal const string TelligentId = "Id";
        internal const string TelligentEmail = "PrivateEmail";

        private static readonly IDictionary<string, PropertyInfo> UserProperties;
        private static readonly IDictionary<string, PropertyInfo> UserUpdateOptionsProperties;

        static ExecuteProfileSyncHelper()
        {
            UserProperties = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo pi in typeof(ApiUser).GetProperties())
            {
                UserProperties.Add(pi.Name, pi);
            }
            UserUpdateOptionsProperties = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo pi in typeof(UsersUpdateOptions).GetProperties())
            {
                UserUpdateOptionsProperties.Add(pi.Name, pi);
            }
        }

        public static void UpdateInternalUser(InternalApi.Entities.TEApiUser user, IEnumerable<string> fields)
        {
            IEnumerable<string> userFields = fields.ToList();
            var optUpdate = new UsersUpdateOptions { Id = Convert.ToInt32(user.Id) };
            try
            {
                var profileFieldList = new ApiList<ProfileField>();
                foreach (string fieldName in userFields)
                {
                    object fieldValue = user[fieldName];
                    string propertyName = fieldName.Replace(" ", String.Empty);
                    if (UserUpdateOptionsProperties.ContainsKey(propertyName))
                    {
                        UserUpdateOptionsProperties[propertyName].SetValue(optUpdate, fieldValue, null);
                    }
                    else
                    {
                        profileFieldList.Add(new ProfileField
                        {
                            Label = GetFieldName(user.Profile, fieldName),
                            Value = fieldValue != null ? fieldValue.ToString() : null
                        });
                    }
                }

                optUpdate.ProfileFields = profileFieldList;
                
                var avatar = user[AvatarUrlField] as string;

                if (userFields.Contains(AvatarUrlField) && !string.IsNullOrEmpty(avatar))
                {
                    optUpdate.ExtendedAttributes = new List<ExtendedAttribute>
                    {
                        new ExtendedAttribute
                        {
                            Key = "avatarUrl",
                            Value = avatar
                        }
                    };
                }

                PublicApi.Users.Update(optUpdate);
            }
            catch (Exception ex)
            {
                var csEx = new CSException(CSExceptionType.UserProfileUpdated, string.Format("Could Not Update Internal User: {0}/{1}", user.Id, user.Email ?? string.Empty), ex);
                csEx.Log();
            }
        }

        public static MergeResult MergeUsers(InternalApi.Entities.User externalUser, InternalApi.Entities.User internalUser, IEnumerable<InternalApi.Entities.UserFieldMapping> mapping)
        {
            var res = MergeResult.None;
            var hasNullField = false;
            
            try
            {
                foreach (var map in mapping)
                {
                    var internalFieldVal = internalUser[map.InternalUserFieldId];
                    var externalFieldVal = externalUser[map.ExternalUserFieldId];

                    hasNullField |= internalFieldVal == null;
                    
                    if (map.SyncDirection == InternalApi.Entities.SyncDirection.Export && internalFieldVal != null && !internalFieldVal.Equals(externalFieldVal))
                    {
                        internalUser[map.InternalUserFieldId] = externalUser[map.ExternalUserFieldId];
                        res |= MergeResult.InternalUpdated;
                    }
                    
                    if (map.SyncDirection == InternalApi.Entities.SyncDirection.Import && externalFieldVal != null && !externalFieldVal.Equals(internalFieldVal))
                    {
                        externalUser[map.ExternalUserFieldId] = internalUser[map.InternalUserFieldId];
                        res |= MergeResult.ExternalUpdated;
                    }
                }
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, String.Format("Could Not Merge User: {0}/{1}", internalUser.Id, internalUser.Email ?? String.Empty));
            }

            if (res == MergeResult.None && hasNullField)
            {
                SPLog.Info(string.Format("Profile Sync no fields merged for ({0}:{1}). Please verify __CommunityServer__Service__ contains the Administrators role.", internalUser.Id, internalUser.Email));
            }

            return res;
        }

        internal static Dictionary<string, object> GetInternalUserFields(ApiUser user)
        {
            var fields = new Dictionary<string, object> { { "Bio", user.Bio() } };

            try
            {
                foreach (var property in UserProperties.Values)
                {
                    if (property.Name == "ProfileFields") continue;

                    var fieldName = property.Name.Replace(" ", string.Empty);
                    fields.Add(fieldName, property.GetValue(user, null));
                }

                if (user.ProfileFields == null)
                {
                    throw new Exception(string.Format("The 'ProfileFields' property is null for user {0}. Please verify __CommunityServer__Service__ contains the Administrators role.", user.Id));
                }

                foreach (var field in user.ProfileFields)
                {
                    var fieldName = field.Label.Replace(" ", string.Empty);
                    if (!fields.ContainsKey(fieldName))
                    {
                        fields.Add(fieldName, field.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                SPLog.UserNotFound(ex, ex.Message);
            }
            
            return fields;
        }

        /// <summary>
        /// Returns the correct spacing for a user property. The InProcess API properties contain whitespace and the REST API properties do not.  
        /// </summary>
        /// <param name="user">Api user</param>
        /// <param name="field">Field name for lookup</param>
        /// <returns>Field name with proper spacing</returns>
        private static string GetFieldName(ApiUser user, string field)
        {
            if (user == null || field.Equals(AvatarUrlField, StringComparison.OrdinalIgnoreCase)) return string.Empty;

            var profileField = user.ProfileFields.FirstOrDefault(pf => String.Equals(field, pf.Label.Replace(" ", string.Empty), StringComparison.InvariantCultureIgnoreCase));
            if (profileField != null)
            {
                return profileField.Label;
            }
                
            SPLog.Event(String.Format("Could Not Locate Field Name ({0}) for User({1}).", field, user.Id ?? -1));
            return string.Empty;
        }
    }
}
