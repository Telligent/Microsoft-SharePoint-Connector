(function ($) {
    if (typeof $.telligent === 'undefined')
        $.telligent = {};

    if (typeof $.telligent.evolution === 'undefined')
        $.telligent.evolution = {};

    if (typeof $.telligent.evolution.extensions === 'undefined')
        $.telligent.evolution.extensions = {};

    if (typeof String.prototype.startsWith != 'function') {
        String.prototype.startsWith = function (str) {
            return this.indexOf(str) == 0;
        };
    }

    if (typeof String.prototype.trim != 'function') {
        String.prototype.trim = function () {
            return this.replace(/^\s*([\S\s]*)\b\s*$/, '$1');
        };
    }

    var InvokeSearchGroups = function (textbox, searchText, spinner) {
        if (searchText && searchText.length >= 1) {
            textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            $.telligent.evolution.get({
                url: $.telligent.evolution.site.getBaseUrl() + "api.ashx/v2/groups.json",
                data: {
                    GroupNameFilter: searchText,
                    Permission: 'Group_CreateGroup'
                },
                success: function (response) {
                    if (response && response.Groups.length >= 1) {
                        textbox.glowLookUpTextBox('updateSuggestions',
                            $.map(response.Groups, function (group, i) {
                                var parentGroup = group.ParentGroupId != -1 ? GetParentGroupName(group.ParentGroupId) : "None";
                                var markup = [group.Name, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px; color: #777;' title='", parentGroup, "'>Parent Group: ", parentGroup, "</div>"].join("");
                                return textbox.glowLookUpTextBox('createLookUp', group.Id, group.Name, markup, true);
                            }));
                    }
                }
            });
        }
    },
    GetParentGroupName = function (parentGroupId) {
        var parentName = "None";
        $.telligent.evolution.get({
            url: $.telligent.evolution.site.getBaseUrl() + "api.ashx/v2/groups/{Id}.json",
            data: { Id: parentGroupId },
            async: false,
            success: function (response) {
                parentName = response.Group.Name;
            }
        });
        return parentName;
    },
    init = function (textbox, groupName, spinner) {
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                InvokeSearchGroups(tb, searchText, spinner)
            },
            emptyHtml: '',
            selectedLookUpsHtml: groupName,
            deleteImageUrl: ''
        });
    };
    $.telligent.evolution.extensions.lookupgroup = {
        register: function (context) {
            init(context.LookUpTextBox, context.GroupName, context.Spinner);
        }
    };
})(jQuery);