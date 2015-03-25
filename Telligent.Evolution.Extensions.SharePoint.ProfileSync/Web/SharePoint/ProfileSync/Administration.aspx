<%@ Page Language="C#" AutoEventWireup="true" EnableViewState="false" CodeBehind="Administration.cs" Inherits="Telligent.Evolution.Extensions.SharePoint.ProfileSync.Administration" %>
<%@ Register TagPrefix="SPControl" Namespace="Telligent.Evolution.Extensions.SharePoint.Components.Controls" Assembly="Telligent.Evolution.Extensions.SharePoint.Components, Version=2.0.0.24404, Culture=neutral, PublicKeyToken=null" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"> 
<html>
<head id="AdministrationPageHead" runat="server">
    <link rel="Stylesheet" type="text/css" href="/ControlPanel/style/controlpanel.css">
    <link rel="Stylesheet" type="text/css" href="/SharePoint/ProfileSync/Style/SPProfileSync.css" />
</head>
<body class="sp-profile-sync-modal-body">
    <form id="profileSyncForm" runat="server">
        <script src="Style/Administration.js" type="text/javascript"></script>
        <script type="text/javascript">
            function CloseWindow(returnValue) {
                var opener = window.parent.Telligent_Modal.GetWindowOpener(window);
                if (opener) opener.Telligent_Modal.Close(returnValue);
            }

            $(function (j) {
                var $ddlTEProfileFields = $("#<%= ddlTEProfileFields.ClientID %>");
                $("table.site-user-profile.tbl-field-mapping").find(".te-field-cell").append($ddlTEProfileFields.clone().removeAttr("id").removeAttr("style"));
                $("table.farm-user-profile.tbl-field-mapping").find(".te-field-cell").append($ddlTEProfileFields.clone().removeAttr("id").removeAttr("style"));

                j.telligent.sharepoint.controlPanel.profileSync.register({
                    wrapper                 : j("#user-profile-fields-mapping"),
                    siteProfileFieldsMapping: j("#<%= hdnSiteProfileFieldsMap.ClientID %>"),
                    farmProfileFieldsMapping: j("#<%= hdnFarmProfileFieldsMap.ClientID %>"),
                    farmSyncCheckBox        : j("#<%= cbFarmSyncEnable.ClientID %>"),
                    errorHolder             : j("#errorHolder"),
                    errorMessage            : "Value cannot be null. Parameter name: source"
                });
            });
        </script>

        <asp:HiddenField ID="hdnSiteProfileFieldsMap" runat="server" />
        <asp:HiddenField ID="hdnFarmProfileFieldsMap" runat="server" />
        <asp:DropDownList ID="ddlTEProfileFields" runat="server" style="display:none;"></asp:DropDownList>

        <div class="sp-profile-sync-modal administration">
            <div class="content">
                <table cellspacing="0" cellpadding="2" border="0" width="100%">

                    <%-- Farm Sync Enabled--%>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Select the checkbox to the right if User Profile Service is enabled</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:CheckBox id="cbFarmSyncEnable" runat="server" Text="Enable User Profile Properties"></asp:CheckBox>
                        </td>
                    </tr>

                    <%-- Mapped fields--%>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Using the drop down boxes to the right map the SharePoint User Profile field with a correct Telligent Evolution User Profile field.</strong>
                            <br />
                        </td>
                        <td id="user-profile-fields-mapping" class="CommonFormField">

                            <%-- Site Profile Fields --%>
                            <table class="site-user-profile tbl-field-mapping">
                                <thead>
                                    <tr>
                                        <th>SharePoint Profile Attribute</th>
                                        <th>Action</th>
                                        <th>Telligent Profile Attribute</th>
                                    </tr>
                                </thead>
                                <tfoot>
                                    <tr>
                                        <td colspan="3">
                                            <a class="add-row-link" href="#">Add another user profile attribute...</a>
                                        </td>
                                    </tr>
                                </tfoot>
                                <tbody>
                                    <tr class="template">
                                        <td class="sp-field-cell">
                                            <asp:DropDownList ID="ddlSPSiteProfileFields" runat="server"></asp:DropDownList>
                                        </td>
                                        <td class="action-cell">
                                            <select></select>
                                        </td>
                                        <td class="te-field-cell">
                                        </td>
                                        <td class="del-cell">
                                            <a href="#">
                                                <span></span>Delete
                                            </a>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            

                            <%-- Farm Profile Fields --%>
                            <table class="farm-user-profile tbl-field-mapping">
                                <thead>
                                    <tr>
                                        <th>SharePoint Profile Attribute</th>
                                        <th>Action</th>
                                        <th>Telligent Profile Attribute</th>
                                    </tr>
                                </thead>
                                <tfoot>
                                    <tr>
                                        <td colspan="3">
                                            <a class="add-row-link" href="#">Add another user profile attribute...</a>
                                        </td>
                                    </tr>
                                </tfoot>
                                <tbody>
                                    <tr class="template">
                                        <td class="sp-field-cell">
                                            <asp:DropDownList ID="ddlSPFarmProfileFields" runat="server"></asp:DropDownList>
                                        </td>
                                        <td class="action-cell">
                                            <select></select>
                                        </td>
                                        <td class="te-field-cell">
                                        </td>
                                        <td class="del-cell">
                                            <a href="#">
                                                <span></span>Delete
                                            </a>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </td>
                    </tr>

                    <%-- Sync Enabled --%>
                    <tr valign="top" class="field-item ConsumerKey">
                        <td class="CommonFormFieldName" style="width: 50%">
                            <strong>Select the checkbox to Sync all profiles.</strong>
                            <br />
                        </td>
                        <td class="CommonFormField">
                            <asp:CheckBox id="cbSyncEnable" runat="server" Text="Enable Sync"></asp:CheckBox>
                        </td>
                    </tr>

                </table>
            </div>
        </div>
        <div id="errorHolder" style="color: Red; padding: 8px 2px 2px 15px;">
            <asp:Literal ID="ErrorMessage" runat="server"/>
        </div>
        <asp:LinkButton ID="SaveBtn" runat="server" CssClass="CommonTextButton save-button" OnClick="SaveBtnClick">
            Save
        </asp:LinkButton>
        <asp:LinkButton ID="ApplyBtn" runat="server" CssClass="CommonTextButton save-button" OnClick="ApplyBtnClick">
            Apply
        </asp:LinkButton>
    </form>
</body>
</html>