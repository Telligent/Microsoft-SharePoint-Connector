using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.Evolution.Controls;

[assembly: WebResource("Telligent.Evolution.Extensions.OpenSearch.PropertyControls.ConfigureProviderControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.OpenSearch
{
    public abstract class ConfigureProviderControl : Control
    {
        protected string CustomStyle = @"<link rel='stylesheet' type='text/css' href='/SharePoint/OpenSearch/Style/OpenSearch.css' />";

        const int WidgetTitleMaxLengthValue = 1000;
        const int MinResultsPerPageValue = 1;
        const int MaxResultsPerPageValue = 1000;

        #region Controls
        protected HtmlGenericControl ContentDiv = new HtmlGenericControl("div");
        protected HtmlGenericControl WidgetTitleDiv = new HtmlGenericControl("div");
        protected TextBox WidgetTitle = new TextBox();
        protected CustomValidator WidgetTitleValidator = new CustomValidator();
        protected HtmlGenericControl ProvidersListDiv = new HtmlGenericControl("div");
        protected DropDownList ProvidersList = new DropDownList();
        protected HtmlGenericControl ResultsPerPageDiv = new HtmlGenericControl("div");
        protected TextBox ResultsPerPage = new TextBox();
        protected CustomValidator ResultsPerPageValidator = new CustomValidator();
        protected HtmlGenericControl ShowMoreResultsDiv = new HtmlGenericControl("div");
        protected CheckBox ShowMoreResultsLink = new CheckBox();
        protected HtmlGenericControl TextonlyResultsDiv = new HtmlGenericControl("div");
        protected CheckBox TextonlyResults = new CheckBox();
        protected HtmlGenericControl ErrorMessageDiv = new HtmlGenericControl("p");
        protected ValidationSummary ValidationSummary = new ValidationSummary();
        #endregion

        private readonly OpenSearchPlugin plugin = OpenSearchPlugin.Plugin;

        #region Control Overrides
        protected override void OnInit(EventArgs e)
        {
            Page.ViewStateMode = ViewStateMode.Enabled;
            Page.EnableViewState = true;
            base.OnInit(e);
            EnsureChildControls();
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(ConfigureProviderControl),
                "Telligent.Evolution.Extensions.OpenSearch.PropertyControls.ConfigureProviderControl.js");
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            ContentDiv.Attributes["class"] = "os-configuraion";

            AddControl(plugin.GetResourceString("configuration_widgettitle"), WidgetTitleDiv, WidgetTitle);
            CreateWidgetTitleValidator(WidgetTitleDiv);

            AddControl(plugin.GetResourceString("configuration_providername"), ProvidersListDiv, ProvidersList);
            ProvidersList.Attributes["onchange"] = String.Format("ProviderChange(this)");
            ProvidersList.ID = "providersList";
            ProvidersList.EnableViewState = true;
            ProvidersList.ViewStateMode = ViewStateMode.Enabled;

            AddControl(plugin.GetResourceString("configuration_resultsperpage"), ResultsPerPageDiv, ResultsPerPage);
            CreateResultsPerPageValidator(ResultsPerPageDiv);

            AddControl(plugin.GetResourceString("configuration_textonly"), TextonlyResultsDiv, TextonlyResults);

            AddControl(plugin.GetResourceString("configuration_showmore"), ShowMoreResultsDiv, ShowMoreResultsLink);
            ShowMoreResultsDiv.Attributes["id"] = "showMore";
            ContentDiv.Controls.Add(ErrorMessageDiv);
            ErrorMessageDiv.Visible = false;
            ErrorMessageDiv.Attributes["class"] = "error";

            ContentDiv.Controls.Add(ValidationSummary);
            ValidationSummary.Attributes["class"] = "error";

            Controls.Add(new LiteralControl(CustomStyle));
            Controls.Add(ContentDiv);
        }
        #endregion

        protected void ShowError(string text)
        {
            ErrorMessageDiv.Visible = true;
            ErrorMessageDiv.InnerText = text;
        }

        #region Validation
        private void CreateWidgetTitleValidator(Control ownerControl)
        {
            WidgetTitle.CausesValidation = true;
            WidgetTitleValidator.ControlToValidate = WidgetTitle.ID;
            WidgetTitleValidator.ServerValidate += WidgetTitleValidatorServerValidate;
            WidgetTitleValidator.ErrorMessage = String.Format("The widget title is too long! It should contains not more than \"{0}\" characters.", WidgetTitleMaxLengthValue);
            WidgetTitleValidator.ToolTip = String.Format("The widget title is too long! It should contains not more than \"{0}\" characters.", WidgetTitleMaxLengthValue);
            WidgetTitleValidator.Text = "*";
            ownerControl.Controls.Add(WidgetTitleValidator);
        }

        private void WidgetTitleValidatorServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = WidgetTitle.Text != null && WidgetTitle.Text.Length <= WidgetTitleMaxLengthValue;
        }

        private void CreateResultsPerPageValidator(Control ownerControl)
        {
            ResultsPerPage.CausesValidation = true;
            ResultsPerPageValidator.ControlToValidate = ResultsPerPage.ID;
            ResultsPerPageValidator.ServerValidate += ResultsPerPageValidatorServerValidate;
            ResultsPerPageValidator.ErrorMessage = String.Format("The number of results per page should be more than \"{0}\" and less than \"{1}\" including!", MinResultsPerPageValue, MaxResultsPerPageValue);
            ResultsPerPageValidator.ToolTip = String.Format("The number of results per page should be more than \"{0}\" and less than \"{1}\" including!", MinResultsPerPageValue, MaxResultsPerPageValue);
            ResultsPerPageValidator.Text = "*";
            ownerControl.Controls.Add(ResultsPerPageValidator);
        }

        private void ResultsPerPageValidatorServerValidate(object source, ServerValidateEventArgs args)
        {
            int pages;
            if (int.TryParse(ResultsPerPage.Text, out pages))
            {
                args.IsValid = pages >= MinResultsPerPageValue && pages <= MaxResultsPerPageValue;
            }
            else
            {
                args.IsValid = false;
            }
        }
        #endregion

        #region Utility methods
        private void AddControl(string text, HtmlGenericControl wrapperDivControl, Control inputControl)
        {
            wrapperDivControl.Controls.Add(new HtmlGenericControl("p")
            {
                InnerText = text
            });
            wrapperDivControl.Controls.Add(inputControl);
            ContentDiv.Controls.Add(wrapperDivControl);
        }
        #endregion
    }
}
