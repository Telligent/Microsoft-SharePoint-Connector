<%@ Page Language="C#" AutoEventWireup="true" EnableViewState="false" CodeBehind="Configuration.cs" Inherits="Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Configuration" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"> 
<html>
<head id="SPObjectManagerPageHead" runat="server">
    <link rel="Stylesheet" type="text/css" href="/ControlPanel/style/controlpanel.css"> 
    <link rel="Stylesheet" type="text/css" href="/SharePoint/IntegrationManager/Style/IntegrationManager.css" />
</head>
<body class="sp-object-manager-editpage">
    <form id="formOpenSearchProvider" runat="server" enctype="multipart/form-data">
        <script type="text/javascript">
            function CloseWindow(returnValue) {
                var opener = window.parent.Telligent_Modal.GetWindowOpener(window);
                if (opener) opener.Telligent_Modal.Close(returnValue);
            }
        </script>
        <div class="sp-object-manager-page" id="ObjectManagerDiv" runat="server">
            <div class="CommonFormDescription"></div>
            <div class="content">
                <table cellspacing="0" cellpadding="2" border="0" width="100%">
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>SharePoint 2010/2013 Site Collection URL</strong>
                            <div class="description">
                                <small>Use OAuth for Office365 (SharePoint Online)</small>
                            </div>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbSPSiteUrl" runat="server" EnableViewState="true" CssClass="control"/>
                            <asp:CustomValidator ID="SPSiteCustomValidator" runat="server" 
                                Text="*" ControlToValidate="TbSPSiteUrl" OnServerValidate="SPSiteUrlServerValidate"/>
                            <asp:RequiredFieldValidator ID="SPSiteRequiredFieldValidator" runat="server" 
                                Text="*" ControlToValidate="TbSPSiteUrl" />
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Default</strong><br />
                        </td>
                        <td class="CommonFormField">
                            <asp:CheckBox class="control" ID="CbIsDefault" runat="server" CssClass="control"/>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Authentication</strong><br />
                        </td>
                        <td class="CommonFormField">
                            <div id="CtAuth" class="control" runat="server"></div>
                        </td>
                    </tr>
                </table>
                <asp:ValidationSummary ID="ValidationSummary" runat="server"/>
            </div>
        </div>
        <asp:LinkButton ID="SaveBtn" runat="server" OnClick="SaveBtnClick" CssClass="CommonTextButton save-button">
            Save
        </asp:LinkButton>
    </form>
</body>
</html>