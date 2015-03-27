using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPListItem : ApiEntity, IContent
    {
        private static readonly IListService listService = ServiceLocator.Get<IListService>();
        private static readonly IListItemUrls listItemUrls = ServiceLocator.Get<IListItemUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        private readonly ListItem listItem;
        private readonly List<Field> fields;
        private string url;
        private readonly Regex xHtml = new Regex(@"<[^>]*>", RegexOptions.Singleline | RegexOptions.Compiled);

        public SPListItem() { }

        public SPListItem(Guid id)
        {
            UniqueId = id;
        }

        public SPListItem(ListItem listItem, List<Field> fields)
        {
            try
            {
                this.fields = fields;
                this.listItem = listItem;

                var spAuthor = Value("Author") as FieldUserValue;
                if (spAuthor != null)
                {
                    Author = new Author(spAuthor.LookupId);
                }

                var spEditor = Value("Editor") as FieldUserValue;
                if (spEditor != null)
                {
                    Editor = new Author(spEditor.LookupId);
                }

                CreatedDate = Value("Created") is DateTime ? ((DateTime)Value("Created")).ToLocalTime() : new DateTime();
                Modified = Value("Modified") is DateTime ? ((DateTime)Value("Modified")).ToLocalTime() : new DateTime();

                DisplayName = listItem.IsPropertyAvailable("DisplayName") ? listItem.DisplayName : String.Empty;
                Fields = new Dictionary<string, object>();
                Id = listItem.Id;
                ListId = listItem.ParentList.Id;
                UniqueId = (listItem["UniqueId"] != null) ? Guid.Parse(listItem["UniqueId"].ToString()) : Guid.Empty;

                if (Author != null)
                {
                    AvatarUrl = Author.AvatarUrl;
                }
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, ex.Message);
            }
        }

        [Documentation(Description = "List Item counter Id")]
        public int Id { get; internal set; }

        [Documentation(Description = "List Item unique Id (ContentId)")]
        public Guid UniqueId { get; internal set; }

        [Documentation(Description = "Creator profile")]
        public Author Author { get; private set; }

        [Documentation(Description = "Editor profile")]
        public Author Editor { get; private set; }

        public DateTime CreatedDate { get; private set; }

        public DateTime Modified { get; private set; }

        public Dictionary<string, object> Fields { get; set; }

        [Documentation(Description = "Parent List Id (ApplicationId)")]
        public Guid ListId { get; internal set; }

        [Documentation(Description = "List item title")]
        public string DisplayName { get; internal set; }

        [Documentation(Description = "SharePoint Url for the parent List")]
        public string ListUrl { get { return listItem != null ? listItem.Context.Url : string.Empty; } }

        [Documentation(Description = "Returns a collection of fields, which could be modified")]
        public List<Field> EditableFields()
        {
            return (from Field field in fields
                    where !field.ReadOnlyField
                    select field).ToList();
        }

        public string ValueAsHtml(string fieldName)
        {
            string value;
            if (!listItem.FieldValuesAsHtml.FieldValues.TryGetValue(fieldName, out value))
            {
                value = this[fieldName];
            }
            return value;
        }

        public string ValueAsText(string fieldName)
        {
            return ValueAsText(fieldName, false);
        }

        public string ValueAsText(string fieldName, bool stripHtmlTags)
        {
            string value;
            if (!listItem.FieldValuesAsText.FieldValues.TryGetValue(fieldName, out value))
            {
                return HttpUtility.HtmlEncode(FieldValueToText(fieldName, Value(fieldName)));
            }

            if (stripHtmlTags && !string.IsNullOrEmpty(value))
            {
                value = xHtml.Replace(value, string.Empty);
            }
            return value;
        }

        public string ValueForEdit(string fieldName)
        {
            string value;
            if (!listItem.FieldValuesForEdit.FieldValues.TryGetValue(fieldName, out value))
            {
                value = this[fieldName];
            }
            return value;
        }

        public object Value(string fieldName)
        {
            return listItem != null && listItem.FieldValues.ContainsKey(fieldName) ? listItem.FieldValues[fieldName] : null;
        }

        public bool HasValue(string fieldName)
        {
            var field = fields.FirstOrDefault(f => f.InternalName == fieldName);
            if (field == null) return false;

            var hasDefaultValue = !string.IsNullOrEmpty(field.DefaultValue);
            if (hasDefaultValue) return true;

            var fieldValue = Value(fieldName);
            var hasValue = fieldValue != null;
            if (hasValue)
            {
                // Check that the field value is not empty
                if (field.FieldTypeKind == FieldType.Choice || field.FieldTypeKind == FieldType.MultiChoice)
                {
                    var choices = fieldValue as IEnumerable<string>;
                    if (choices != null)
                    {
                        hasValue = choices.Any();
                    }
                }
                else if (field.FieldTypeKind == FieldType.User)
                {
                    if (fieldValue is FieldUserValue)
                    {
                        var userName = ((FieldUserValue)fieldValue).LookupValue;
                        hasValue = !string.IsNullOrEmpty(userName);
                    }
                    else if (fieldValue is IEnumerable<FieldUserValue>)
                    {
                        hasValue = ((IEnumerable<FieldUserValue>)fieldValue).Any();
                    }
                }
                else if (field.FieldTypeKind == FieldType.Lookup)
                {
                    if (fieldValue is FieldLookupValue)
                    {
                        hasValue = !string.IsNullOrEmpty(((FieldLookupValue)fieldValue).LookupValue);
                    }
                    else if (fieldValue is FieldLookupValue[])
                    {
                        hasValue = ((FieldLookupValue[])fieldValue).Any();
                    }
                }
                else if (field.FieldTypeKind == FieldType.URL && fieldValue is FieldUrlValue)
                {
                    hasValue = !string.IsNullOrEmpty(((FieldUrlValue)fieldValue).Url);
                }
                else if (field.FieldTypeKind == FieldType.Attachments && fieldValue is bool)
                {
                    hasValue = (bool)fieldValue;
                }
                else if (field.FieldTypeKind == FieldType.Invalid)
                {
                    if (fieldValue is IEnumerable<object>)
                    {
                        hasValue = ((IEnumerable<object>)fieldValue).Any();
                    }
                    else
                    {
                        hasValue = !string.IsNullOrEmpty(fieldValue.ToString());
                    }
                }
            }
            return hasValue;
        }

        public string this[string fieldName]
        {
            get
            {
                return RenderFieldValue(fieldName, Value(fieldName));
            }
        }

        #region IContent

        public IApplication Application
        {
            get { return listService.Get(new ListGetQuery(ListId, ListApplicationType.Id)); }
        }

        public Guid ContentId
        {
            get
            {
                return UniqueId;
            }
            internal set
            {
                UniqueId = value;
            }
        }

        public Guid ContentTypeId
        {
            get { return ItemContentType.Id; }
        }

        private int? createdByUserId;
        public int? CreatedByUserId
        {
            get
            {
                if (Author != null && !string.IsNullOrEmpty(Author.Email) && !createdByUserId.HasValue)
                {
                    var user = TEApi.Users.Get(new UsersGetOptions { Email = Author.Email });
                    createdByUserId = user != null ? user.Id : null;
                }
                return createdByUserId;
            }
        }

        public string HtmlName(string target)
        {
            if (!string.IsNullOrEmpty(DisplayName))
            {
                return HttpUtility.HtmlEncode(DisplayName);
            }
            return string.Empty;
        }

        public string HtmlDescription(string target)
        {
            return string.Empty;
        }

        public bool IsEnabled { get { return true; } }

        public string AvatarUrl { get; private set; }

        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(url))
                {
                    var listBase = listDataService.Get(ListId);
                    if (listBase != null)
                    {
                        url = listItemUrls.ViewListItem(listBase, new ItemUrlQuery(Id, ContentId));
                    }
                }
                return url;
            }
            internal set { url = value; }
        }

        #endregion

        #region FieldValueToText

        private string FieldValueToText(string fieldName, object fieldValue)
        {
            var field = fields.FirstOrDefault(f => f.InternalName == fieldName);
            if (field != null)
            {
                if (fieldValue == null)
                {
                    return field.DefaultValue;
                }
                else
                {
                    switch (field.FieldTypeKind)
                    {
                        case FieldType.Number: return NumberToText(field, fieldValue);
                        case FieldType.Currency: return CurrencyToText(field, fieldValue);
                        case FieldType.Choice: return ChoiceToText(field, fieldValue);
                        case FieldType.Note: return NoteToText(field, fieldValue);
                        case FieldType.MultiChoice: return ChoiceToText(field, fieldValue);
                        case FieldType.DateTime: return DateTimeToText(field, fieldValue);
                        case FieldType.User: return UserProfileToText(field, fieldValue);
                        case FieldType.Lookup: return LookupToText(field, fieldValue);
                        case FieldType.URL: return UrlToText(field, fieldValue);
                        case FieldType.Invalid: return InvalidToText(field, fieldValue);
                        default: return fieldValue.ToString();
                    }
                }
            }
            return string.Empty;
        }

        private static string NoteToText(Field field, object fieldValue)
        {
            var text = fieldValue != null ? fieldValue.ToString() : field.DefaultValue;
            if (text != null && text.StartsWith("<p>") && text.EndsWith("</p>"))
            {
                text = text.Substring("<p>".Length, text.Length - "<p>".Length - "</p>".Length);
            }
            return text;
        }

        private static string NumberToText(Field field, object fieldValue)
        {
            var showAsPercentage = false;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(field.SchemaXml);
                var percentage = doc.FirstChild.Attributes["Percentage"].Value;
                var p = false;
                showAsPercentage = !string.IsNullOrEmpty(percentage) && bool.TryParse(percentage, out p) && p;
            }
            catch { }
            return showAsPercentage ? string.Format("{0} %", (int)(100 * (double)(fieldValue != null ? fieldValue : field.DefaultValue))) : fieldValue.ToString();
        }

        private static string CurrencyToText(Field field, object fieldValue)
        {
            if (field is FieldCurrency && fieldValue != null)
            {
                var currencyField = (FieldCurrency)field;
                double currencyValue = (double)fieldValue;
                return currencyValue.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo(currencyField.CurrencyLocaleId));
            }
            return field.DefaultValue;
        }

        private static string ChoiceToText(Field field, object fieldValue)
        {
            var fieldValueToShow = (fieldValue != null ? fieldValue : field.DefaultValue) ?? string.Empty;
            var selectedChoices = fieldValueToShow as IEnumerable<string> ?? new string[] { fieldValueToShow.ToString() };
            return string.Join(", ", selectedChoices);
        }

        private static string DateTimeToText(Field field, object fieldValue)
        {
            if (fieldValue is DateTime)
            {
                return TEApi.Language.FormatDate((DateTime)fieldValue);
            }
            return field.DefaultValue;
        }

        private static string UserProfileToText(Field field, object fieldValue)
        {
            if (fieldValue is FieldUserValue)
            {
                return fieldValue != null ? ((FieldUserValue)fieldValue).LookupValue : field.DefaultValue;
            }
            else if (fieldValue is IEnumerable<FieldUserValue>)
            {
                return string.Join(", ", ((IEnumerable<FieldUserValue>)fieldValue).Select(f => f.LookupValue).ToList());
            }
            return field.DefaultValue;
        }

        private static string LookupToText(Field field, object fieldValue)
        {
            if (fieldValue is FieldLookupValue)
            {
                return ((FieldLookupValue)fieldValue).LookupValue;
            }
            else if (fieldValue is FieldLookupValue[])
            {
                return string.Join(", ", ((FieldLookupValue[])fieldValue).Select(f => f.LookupValue).ToList());
            }
            return field.DefaultValue;
        }

        private static string UrlToText(Field field, object fieldValue)
        {
            if (fieldValue is FieldUrlValue)
            {
                return ((FieldUrlValue)fieldValue).Description;
            }
            return field.DefaultValue;
        }

        private static string InvalidToText(Field field, object fieldValue)
        {
            if (string.Equals(field.TypeAsString, "TaxonomyFieldTypeMulti", StringComparison.InvariantCultureIgnoreCase))
            {
                var terms = new List<string>();
                if (fieldValue is IEnumerable<object>)
                {
                    terms.AddRange(((IEnumerable<object>)fieldValue).Select(v => v.ToString()));
                    if (terms.Count == 0 && !string.IsNullOrEmpty(field.DefaultValue))
                    {
                        terms.AddRange(field.DefaultValue.Split(';'));
                    }
                }
                return string.Join(", ", terms.Where(term => term.Contains('|')).Select(term => term.Split('|')[0].TrimStart('#')));
            }
            else if (string.Equals(field.TypeAsString, "TaxonomyFieldType", StringComparison.InvariantCultureIgnoreCase))
            {
                return fieldValue == null && !string.IsNullOrEmpty(field.DefaultValue) ? field.DefaultValue.Split('|')[0].TrimStart('#') : fieldValue.ToString().Split('|')[0].TrimStart('#');
            }
            return field.DefaultValue;
        }

        #endregion

        #region Rendering

        private string RenderFieldValue(string fieldName, object fieldValue)
        {
            var field = fields.FirstOrDefault(f => f.InternalName == fieldName);
            if (field != null)
            {
                var html = new StringBuilder();
                html.Append("<div class=\"field-type-kind ").Append(Enum.GetName(field.FieldTypeKind.GetType(), field.FieldTypeKind).ToLowerInvariant()).Append("\" >");
                switch (field.FieldTypeKind)
                {
                    case FieldType.Number: RenderNumber(html, field, fieldValue); break;
                    case FieldType.Currency: RenderCurrency(html, field, fieldValue); break;
                    case FieldType.Choice: RenderChoice(html, field, fieldValue); break;
                    case FieldType.MultiChoice: RenderChoice(html, field, fieldValue); break;
                    case FieldType.Note: RenderNote(html, field, fieldValue); break;
                    case FieldType.DateTime: RenderDateTime(html, field, fieldValue); break;
                    case FieldType.User: RenderUserProfile(ListId, html, field, fieldValue); break;
                    case FieldType.Lookup: RenderLookup(html, field, fieldValue); break;
                    case FieldType.URL: RenderUrl(html, field, fieldValue); break;
                    case FieldType.Attachments: RenderAttachments(html, fieldName, fieldValue); break;
                    case FieldType.Invalid: RenderInvalid(html, field, fieldValue); break;
                    default: RenderText(html, field, fieldValue); break;
                }
                html.Append("</div>");
                return html.ToString();
            }
            return string.Empty;
        }

        private static void RenderText(StringBuilder html, Field field, object fieldValue)
        {
            html.Append(HttpUtility.HtmlEncode(fieldValue != null ? fieldValue.ToString() : field.DefaultValue));
        }

        private static void RenderNote(StringBuilder html, Field field, object fieldValue)
        {
            var text = fieldValue != null ? fieldValue.ToString() : field.DefaultValue;
            if (text != null && text.StartsWith("<p>") && text.EndsWith("</p>"))
            {
                text = text.Substring("<p>".Length, text.Length - "<p>".Length - "</p>".Length);
            }
            html.Append("<note>").Append(HttpUtility.HtmlEncode(text)).Append("</note>");
        }

        private static void RenderNumber(StringBuilder html, Field field, object fieldValue)
        {
            var showAsPercentage = false;
            try
            {
                var fieldNumber = (FieldNumber)field;
                var doc = new XmlDocument();
                doc.LoadXml(field.SchemaXml);
                var percentage = doc.FirstChild.Attributes["Percentage"].Value;
                var p = false;
                showAsPercentage = !string.IsNullOrEmpty(percentage) && bool.TryParse(percentage, out p) && p;
            }
            catch { }

            double numberValue = 0;
            double numberDefaultValue;
            if (fieldValue != null)
            {
                numberValue = (double)fieldValue;
            }
            else if (field.DefaultValue != null && double.TryParse(field.DefaultValue, out numberDefaultValue))
            {
                numberValue = numberDefaultValue;
            }

            if (showAsPercentage)
            {
                html.AppendFormat("{0} %", (int)(100 * numberValue));
            }
            else
            {
                html.Append(numberValue);
            }
        }

        private static void RenderCurrency(StringBuilder html, Field field, object fieldValue)
        {
            if (field is FieldCurrency)
            {
                var currencyField = (FieldCurrency)field;
                double currencyValue = 0;
                double currencyDefaultValue;
                if (fieldValue != null)
                {
                    currencyValue = (double)fieldValue;
                }
                else if (field.DefaultValue != null && double.TryParse(field.DefaultValue, out currencyDefaultValue))
                {
                    currencyValue = currencyDefaultValue;
                }
                html.Append(currencyValue.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo(currencyField.CurrencyLocaleId)));
            }
        }

        private static void RenderChoice(StringBuilder html, Field field, object fieldValue)
        {
            var fieldValueToShow = (fieldValue != null ? fieldValue : field.DefaultValue) ?? string.Empty;
            var selectedChoices = fieldValueToShow as IEnumerable<string> ?? new string[] { fieldValueToShow.ToString() };
            if (field is FieldChoice)
            {
                var fieldChoice = (FieldChoice)field;
                RenderChoice(html, Enum.GetName(fieldChoice.EditFormat.GetType(), fieldChoice.EditFormat), fieldChoice.Choices, selectedChoices);
            }
            else if (field is FieldMultiChoice)
            {
                var fieldChoice = (FieldMultiChoice)field;
                RenderChoice(html, "checkbox", fieldChoice.Choices, selectedChoices);
            }
        }

        private static void RenderChoice(StringBuilder html, string fieldChoiceType, IEnumerable<string> availableChoices, IEnumerable<string> selectedChoices)
        {
            html.Append("<ul class=\"choice-list " + fieldChoiceType.ToLowerInvariant() + "\" >");
            foreach (var choice in availableChoices)
            {
                html.Append("<li class=\"choice-item ").Append(selectedChoices.Contains(choice) ? "selected" : string.Empty).Append("\" >");
                html.Append(HttpUtility.HtmlEncode(choice));
                html.Append("</li>");
            }
            html.Append("</ul>");
        }

        private static void RenderDateTime(StringBuilder html, Field field, object fieldValue)
        {
            if (fieldValue is DateTime)
            {
                html.Append(TEApi.Language.FormatDate((DateTime)fieldValue));
            }
            else
            {
                html.Append(field.DefaultValue);
            }
        }

        private static void RenderUserProfile(Guid listId, StringBuilder html, Field field, object fieldValue)
        {
            if (fieldValue is FieldUserValue)
            {
                html.Append("<div class=\"").Append(field.TypeAsString.ToLowerInvariant()).Append("\" >");
                BuildUserProfileMarkup(listId, html, (FieldUserValue)fieldValue);
                html.Append("</div>");
            }
            else if (fieldValue is IEnumerable<FieldUserValue>)
            {
                html.Append("<ul class=\"user-profile-list ").Append(field.TypeAsString.ToLowerInvariant()).Append("\" >");
                foreach (FieldUserValue userProfileFieldValue in (IEnumerable<FieldUserValue>)fieldValue)
                {
                    html.Append("<li class=\"user-profile-item\" >");
                    BuildUserProfileMarkup(listId, html, userProfileFieldValue);
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }
        }

        private static void BuildUserProfileMarkup(Guid listId, StringBuilder html, FieldUserValue userProfileField)
        {
            html.Append("<span class=\"user-name\">");

            var userProfileId = userProfileField.LookupId;
            var userProfile = PublicApi.UserProfiles.Get(listId, userProfileId);
            if (!string.IsNullOrEmpty(userProfile.Email))
            {
                var evolutionUser = TEApi.Users.Get(new UsersGetOptions { Email = userProfile.Email });
                if (evolutionUser != null)
                {
                    html.AppendFormat("<a href=\"{0}\" class=\"user-icon internal-link view-user-profile\">{1}</a>", HttpUtility.HtmlAttributeEncode(evolutionUser.ProfileUrl), HttpUtility.HtmlEncode(evolutionUser.DisplayName));
                }
                else
                {
                    html.AppendFormat("<span class=\"user-icon\" title=\"{0}\">{1}</span>", HttpUtility.HtmlAttributeEncode(userProfileField.LookupValue), HttpUtility.HtmlEncode(userProfileField.LookupValue));
                }
            }
            else
            {
                html.Append(HttpUtility.HtmlEncode(userProfileField.LookupValue));
            }

            html.Append("</span>");
        }

        private static void RenderLookup(StringBuilder html, Field field, object fieldValue)
        {
            if (fieldValue is FieldLookupValue)
            {
                html.Append(((FieldLookupValue)fieldValue).LookupValue);
            }
            else if (fieldValue is FieldLookupValue[])
            {
                html.Append("<ul class=\"field-lookup-list\" >");
                foreach (var fieldLookupValue in (FieldLookupValue[])fieldValue)
                {
                    html.Append("<li class=\"field-lookup-item\" >").Append(HttpUtility.HtmlEncode(fieldLookupValue.LookupValue)).Append("</li>");
                }
                html.Append("</ul>");
            }
            else
            {
                html.Append(field.DefaultValue);
            }
        }

        private static void RenderUrl(StringBuilder html, Field field, object fieldValue)
        {
            if (fieldValue is FieldUrlValue)
            {
                var urlValue = (FieldUrlValue)fieldValue;
                html.Append("<a href=\"").Append(HttpUtility.JavaScriptStringEncode(urlValue.Url)).Append("\" target=\"blank\">").Append(HttpUtility.HtmlEncode(urlValue.Description)).Append("</a>");
            }
            else
            {
                html.Append(field.DefaultValue);
            }
        }

        private void RenderAttachments(StringBuilder html, string fieldName, object fieldValue)
        {
            if (fieldValue is bool && (bool)fieldValue)
            {
                html.Append("<ul class=\"attachment-list\" >");
                foreach (var attachment in PublicApi.Attachments.List(ListId, new AttachmentsGetOptions(ContentId, fieldName)))
                {
                    html.Append("<li class=\"attachment-item\" >");
                    html.Append("<a class=\"attachment-link\" href=\"").Append(HttpUtility.JavaScriptStringEncode(attachment.Uri.ToString())).Append("\" >");
                    html.Append(HttpUtility.HtmlEncode(attachment.Name));
                    html.Append("</a>");
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }
            else
            {
                html.Append("<div class=\"no-records\"></div>");
            }
        }

        private static void RenderInvalid(StringBuilder html, Field field, object fieldValue)
        {
            if (string.Equals(field.TypeAsString, "TaxonomyFieldTypeMulti", StringComparison.InvariantCultureIgnoreCase))
            {
                var terms = new List<string>();
                if (fieldValue is IEnumerable<object>)
                {
                    terms.AddRange(((IEnumerable<object>)fieldValue).Select(ParseTermLabel));
                    if (terms.Count == 0 && !string.IsNullOrEmpty(field.DefaultValue))
                    {
                        terms.AddRange(field.DefaultValue.Split(';'));
                    }
                }
                RenderTaxonomyItems(html, field.TypeAsString, terms);
            }
            else if (string.Equals(field.TypeAsString, "TaxonomyFieldType", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fieldValue == null)
                {
                    if (!string.IsNullOrEmpty(field.DefaultValue))
                    {
                        RenderTaxonomyItems(html, field.TypeAsString, new[] { field.DefaultValue });
                    }
                }
                else
                {
                    RenderTaxonomyItems(html, field.TypeAsString, new[] { ParseTermLabel(fieldValue) });
                }
            }
            else
            {
                html.Append(fieldValue);
            }
        }

        private static string ParseTermLabel(object term)
        {
            if (term is TaxonomyFieldValue)
            {
                var taxonomyTerm = (TaxonomyFieldValue)term;
                return taxonomyTerm.Label;
            }
            else
            {
                Guid id;
                var termNameIdPair = term.ToString().Split('|');
                if (termNameIdPair.Length == 2
                    && !string.IsNullOrEmpty(termNameIdPair[0])
                    && Guid.TryParse(termNameIdPair[1], out id))
                    return termNameIdPair[0];
                return null;
            }
        }


        private static void RenderTaxonomyItems(StringBuilder html, string className, IEnumerable<string> termLabels)
        {
            html.Append("<ul class=\"taxonomy-term-list ").Append(className.ToLowerInvariant()).Append("\" >");
            foreach (var label in termLabels)
            {
                html.Append("<li class=\"taxonomy-term-item\" ").Append("\" >").Append(HttpUtility.HtmlEncode(label)).Append("</li>");
            }

            html.Append("</ul>");
        }

        private static void RenderTaxonomyItems(StringBuilder html, string className, IEnumerable<Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValue> terms)
        {
            html.Append("<ul class=\"taxonomy-term-list ").Append(className.ToLowerInvariant()).Append("\" >");
            foreach (var term in terms)
            {
                html.Append("<li class=\"taxonomy-term-item\" ").Append("data-id=\"").Append(term.TermGuid).Append("\" >").Append(HttpUtility.HtmlEncode(term.Label)).Append("</li>");
            }
            html.Append("</ul>");
        }

        #endregion
    }
}
