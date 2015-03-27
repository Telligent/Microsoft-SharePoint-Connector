<%@ Page Language="C#" AutoEventWireup="true" EnableViewState="true" CodeBehind="OpenSearchProviderPage.cs"
    Inherits="Telligent.Evolution.Extensions.OpenSearch.OpenSearchProviderPage" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN">
<html>
<head id="PageHead" runat="server">
    <title><%= PageTitle %></title>
    <%= PageHeaders %>
    <link rel="Stylesheet" type="text/css" href="/ControlPanel/style/controlpanel.css">
    <link rel="Stylesheet" type="text/css" href="/SharePoint/OpenSearch/Style/OpenSearch.css" />
</head>
<body class="os-provider-editpage">
    <form id="formOpenSearchProvider" runat="server" enctype="multipart/form-data">
        <script type="text/javascript">
            function CloseWindow(returnValue) {
                var opener = window.parent.Telligent_Modal.GetWindowOpener(window);
                if (opener) opener.Telligent_Modal.Close(returnValue);
            }
        </script>
        <div class="open-search-provider-page" id="openSearchDiv" runat="server">
            <div class="CommonFormDescription"></div>
            <div class="content">
                <table cellspacing="0" cellpadding="2" border="0" width="100%">
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Name</strong>
                            <asp:CustomValidator ID="nameCustomValidator" runat="server" Text="*" ControlToValidate="tbName"
                                    OnServerValidate="NameCustomValidatorServerValidate" />
                            <asp:RequiredFieldValidator ID="providerNameRequiredFieldValidator" runat="server"
                                Text="*" ControlToValidate="tbName" />
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="tbName" runat="server" EnableViewState="true" CssClass="control"
                                CausesValidation="true" ViewStateMode="Enabled"></asp:TextBox>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>OSDX</strong>
                            <asp:CustomValidator ID="fileUploaderCustomValidator" runat="server" Text="*" ControlToValidate="OSDXFileUpload"
                                OnServerValidate="FileUploaderCustomValidatorServerValidate" />
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:FileUpload ID="OSDXFileUpload" runat="server" CssClass="control" ValidationGroup="OpenSearch" />
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Authentication</strong><br />
                        </td>
                        <td class="CommonFormField">
                            <div id="ctAuth" class="control" runat="server"></div>
                        </td>
                    </tr>
                </table>
                <asp:ValidationSummary ID="validationSummary" runat="server" />
            </div>
        </div>
        <asp:LinkButton ID="SaveBtn" runat="server" OnClick="SaveBtnClick" CssClass="CommonTextButton save-button">
            Save
        </asp:LinkButton>
    </form>
</body>
</html>
