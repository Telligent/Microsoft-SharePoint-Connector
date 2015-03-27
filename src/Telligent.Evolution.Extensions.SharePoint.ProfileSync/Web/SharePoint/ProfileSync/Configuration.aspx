<%@ Page Language="C#" AutoEventWireup="true" EnableViewState="false" CodeBehind="Configuration.cs" Inherits="Telligent.Evolution.Extensions.SharePoint.ProfileSync.Configuration" %>
<%@ Register TagPrefix="SPControl" Namespace="Telligent.Evolution.Extensions.SharePoint.Components.Controls" Assembly="Telligent.Evolution.Extensions.SharePoint.Components, Version=2.0.0.24404, Culture=neutral, PublicKeyToken=null" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"> 
<html>
<head runat="server">
    <link rel="Stylesheet" type="text/css" href="/ControlPanel/style/controlpanel.css">
    <link rel="Stylesheet" type="text/css" href="/SharePoint/ProfileSync/Style/SPProfileSync.css" />
</head>
<body class="sp-profile-sync-modal-body">
    <form id="profileSyncForm" runat="server" enctype="multipart/form-data">
        <asp:HiddenField ID="HdnFarmSyncEnabled" runat="server"/>
        <script type="text/javascript">
            function CloseWindow(returnValue) {
                var opener = window.parent.Telligent_Modal.GetWindowOpener(window);
                if (opener) opener.Telligent_Modal.Close(returnValue);
            }

            $(function () {
                var init = function() {
                    refreshProfileSettings();
                    attachHandlers();
                },
                attachHandlers = function() {
                    $("input[name=profile-settings]").click(function () {
                        refreshProfileSettings();
                    });
                },
                refreshProfileSettings = function () {
                    if($("input[name=profile-settings]:checked").val() == "site") {
                        $(".site-profile-settings").show();
                        $(".farm-profile-settings").hide();
                    }
                    else{
                        $(".site-profile-settings").hide();
                        $(".farm-profile-settings").show();
                    };
                };

                init();
            });
        </script>
        <div class="sp-profile-sync-modal">
            <div class="content">
                <table cellspacing="0" cellpadding="2" border="0" width="100%">
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>SharePoint Site Collection URL</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbSPSiteUrl" runat="server" EnableViewState="true" CssClass="control"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="ReqSPSiteUrl" runat="server" Text="*" ControlToValidate="TbSPSiteUrl" />
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td colspan="2" class="CommonFormFieldName">
                            <label>
                                <input type="radio" name="profile-settings" value="site" <%= !FarmSyncEnabled ? "checked='checked'" : "" %>/>
                                Site Users - User Information List
                            </label>
                            <br/>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey site-profile-settings">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>User ID Field Name</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbUserIdFieldName" runat="server" EnableViewState="true" CssClass="control"></asp:TextBox>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey site-profile-settings">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>User Email Field Name</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbUserEmailFieldName" runat="server" EnableViewState="true" CssClass="control"></asp:TextBox>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td colspan="2" class="CommonFormFieldName">
                            <label>
                                <input type="radio" name="profile-settings" value="farm" <%= FarmSyncEnabled ? "checked='checked'" : "" %>/>

                                Farm Users - User Profile Service
                            </label>
                            <br/>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey farm-profile-settings">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>User ID Field Name</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbFarmUserIdFieldName" runat="server" EnableViewState="true" CssClass="control" Enabled="false"></asp:TextBox>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey farm-profile-settings">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>User Email Field Name</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:TextBox ID="TbFarmUserEmailFieldName" runat="server" EnableViewState="true" CssClass="control"></asp:TextBox>
                        </td>
                    </tr>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Authentication</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <div ID="CtAuth" class="control" runat="server"></div>
                            <asp:CustomValidator ID="CredentialsValidator" runat="server" ErrorMessage="Invalid SharePoint Credentials">
                            </asp:CustomValidator>
                        </td>
                    </tr>
                </table>
                <asp:validationsummary ID="ValidationSummary" runat="server"/>
            </div>
        </div>
        <div style="color: Red;">
            <asp:Literal ID="ErrorMessage" runat="server"/>
        </div>
        <asp:LinkButton ID="SaveBtn" runat="server" CssClass="CommonTextButton save-button" OnClick="SaveBtnClick">
            Save
        </asp:LinkButton>
        <asp:Literal ID="MessageLit" runat="server"/>
    </form>
</body>
</html>