using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class OpenSearchProviderPage : Page
    {
        const string AddProviderTitleText = "Add OpenSearch Providers";
        const string EditProviderTitleText = "Edit OpenSearch Providers";
        const string EmptyProviderNameErrorMessage = "The name of provider can't be empty.";
        const string TooLongProviderNameErrorMessage = "The name of provider is too long! It should contains not more than \"{0}\" characters.";
        const string NotSupportedFileExtensionErrorMessage = "Files with such extension are not supported. You can load files with extension \"{0}\".";
        const string FileSizeLimitErrorMessage = "File size limit is exceeded. Maximum file size is \"{0}\" KB.";
        const string InvalidFileErrorMessage = "Loaded file is invalid.";

        const int NameMaxLength = 100;
        const int MaxFileSize = 2097152; // 2 MB
        const string FileExtension = "OSDX";

        protected string PageHeaders
        {
            get
            {
                var pageHeaders = new StringBuilder();
                pageHeaders.Append(CSControlUtility.Instance().GetJQueryScriptTag(JQueryScript.JQuery));
                return pageHeaders.ToString();
            }
        }

        enum PageMode
        {
            Add, Edit
        }

        private PageMode pageMode;
        public String PageTitle { get; set; }

        protected LinkButton SaveBtn;
        protected TextBox tbName;
        protected FileUpload OSDXFileUpload;
        protected HtmlGenericControl ctAuth;
        protected CustomValidator nameCustomValidator;
        protected CustomValidator fileUploaderCustomValidator;
        protected ValidationSummary validationSummary;
        protected RequiredFieldValidator providerNameRequiredFieldValidator;

        private SearchProvider openSearchProvider { get; set; }

        #region Page Overriden
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureChildControls();
            providerNameRequiredFieldValidator.ErrorMessage = EmptyProviderNameErrorMessage;
            providerNameRequiredFieldValidator.ToolTip = providerNameRequiredFieldValidator.ErrorMessage;
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            pageMode = Request.QueryString["mode"] == "add" ? PageMode.Add : PageMode.Edit;
            if (pageMode == PageMode.Add)
            {
                PageTitle = AddProviderTitleText;
                openSearchProvider = new SearchProvider();
                ctAuth.Controls.Add(AuthenticationHelper.GetPropertyControls());
            }
            else
            {
                PageTitle = EditProviderTitleText;
                openSearchProvider = new SearchProvider(Request.QueryString);
                ctAuth.Controls.Add(AuthenticationHelper.SetPropertyControls(openSearchProvider.Authentication));
            }
            tbName.Text = HttpUtility.HtmlDecode(openSearchProvider.Name);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            openSearchProvider.Authentication = AuthenticationHelper.FromHtml(ctAuth);
            var serviceAccount = openSearchProvider.Authentication as ServiceAccount;
            if (serviceAccount != null)
            {
                (serviceAccount).ValidationEnabled = true;
            }
            ctAuth.Controls.Clear();
            ctAuth.Controls.Add(AuthenticationHelper.SetPropertyControls(openSearchProvider.Authentication));
        }
        #endregion

        protected void SaveBtnClick(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (!String.IsNullOrEmpty(tbName.Text))
                openSearchProvider.Name = HttpUtility.HtmlEncode(tbName.Text);

            openSearchProvider.Authentication = AuthenticationHelper.FromHtml(ctAuth);
            const string script = @"setTimeout(function(){{CloseWindow('{0}');}},100);";
            CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(OpenSearchProviderPage), "closechildwindow",
                string.Format(script, Components.JavaScript.Encode(openSearchProvider.ToXml())), true);
        }

        #region Validation
        protected void NameCustomValidatorServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !String.IsNullOrEmpty(args.Value) && args.Value.Length <= NameMaxLength;
            nameCustomValidator.ErrorMessage = String.Format(TooLongProviderNameErrorMessage, NameMaxLength);
            nameCustomValidator.ToolTip = nameCustomValidator.ErrorMessage;
        }

        protected void FileUploaderCustomValidatorServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = true;
            const int kbSize = 1024;
            const int maxSizeInKB = MaxFileSize / kbSize;

            // check file extension
            var extension = new Regex(@".+\." + FileExtension + @"$", RegexOptions.IgnoreCase);
            if (OSDXFileUpload.HasFile && !extension.IsMatch(OSDXFileUpload.FileName))
            {
                args.IsValid = false;
                fileUploaderCustomValidator.ErrorMessage = String.Format(NotSupportedFileExtensionErrorMessage, FileExtension);
                fileUploaderCustomValidator.ToolTip = fileUploaderCustomValidator.ErrorMessage;
                return;
            }

            // check file size
            if (OSDXFileUpload.HasFile && OSDXFileUpload.PostedFile.ContentLength > MaxFileSize)
            {
                args.IsValid = false;
                fileUploaderCustomValidator.ErrorMessage = String.Format(FileSizeLimitErrorMessage, maxSizeInKB);
                fileUploaderCustomValidator.ToolTip = fileUploaderCustomValidator.ErrorMessage;
                return;
            }

            // parse uploaded file
            try
            {
                UploadOSDXFile(openSearchProvider);
            }
            catch (FormatException)
            {
                args.IsValid = false;
                fileUploaderCustomValidator.ErrorMessage = InvalidFileErrorMessage;
                fileUploaderCustomValidator.ToolTip = fileUploaderCustomValidator.ErrorMessage;
            }
        }
        #endregion

        #region File uploading
        private void UploadOSDXFile(SearchProvider provider)
        {
            if (OSDXFileUpload.HasFile && OSDXFileUpload.PostedFile.ContentLength <= MaxFileSize)
            {
                provider.ProcessOSDXFile(UploadXml(OSDXFileUpload));
            }
        }

        private string UploadXml(FileUpload uploader)
        {
            int fileLength = uploader.PostedFile.ContentLength;
            var byteStream = new byte[fileLength];
            Stream osdxStream = uploader.FileContent;
            osdxStream.Read(byteStream, 0, fileLength);
            osdxStream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(osdxStream);
            return reader.ReadToEnd();
        }
        #endregion
    }
}
