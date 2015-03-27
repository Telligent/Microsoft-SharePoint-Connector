//$.telligent.sharepoint.widgets.documentLibrary.permissionsTabs
(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        listHolderTabId: '',
        grantHolderTabId: '',
        editHolderTabId: ''
    },
    attachHandlers = function (context) {
        $(".grant-permissions", context.listHolderTabId).click(function (e) {
            e.preventDefault();
            if ($(this).hasClass("disabled")) {
                e.stopPropagation();
                return;
            }

            $(context.listHolderTabId).hide();
            $(context.grantHolderTabId).show();
        });

        $(".submit-button.save", context.grantHolderTabId).click(function (e) {
            e.preventDefault();
            if ($(this).hasClass("disabled")) {
                e.stopPropagation();
                return;
            }

            $(context.listHolderTabId).show();
            $(context.grantHolderTabId).hide();
        });

        $(".submit-button.cancel", context.grantHolderTabId).click(function (e) {
            e.preventDefault();
            $(context.listHolderTabId).show();
            $(context.grantHolderTabId).hide();
        });

        $(".edit-permissions", context.listHolderTabId).click(function (e) {
            e.preventDefault();
            if ($(this).hasClass("disabled")) {
                e.stopPropagation();
                return;
            }

            $(context.listHolderTabId).hide();
            $(context.editHolderTabId).show();
        });

        $(".submit-button.save", context.editHolderTabId).click(function (e) {
            e.preventDefault();
            if ($(this).hasClass("disabled")) {
                e.stopPropagation();
                return;
            }

            $(context.listHolderTabId).show();
            $(context.editHolderTabId).hide();
        });

        $(".submit-button.cancel", context.editHolderTabId).click(function (e) {
            e.preventDefault();
            $(context.listHolderTabId).show();
            $(context.editHolderTabId).hide();
        });
    };

    $.telligent.sharepoint.widgets.documentLibrary.permissionsTabs = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };

})(jQuery, window);

//$.telligent.sharepoint.widgets.documentLibrary.listPermissions
(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        wrapper: global.document,
        permissionsListUrl: null,
        updatePermissionsUrl: null,
        pagedListHolderId: ''
    },
    eventNames = {
        grantPermissions: "grant-permissions",
        editPermissions: "edit-permissions"
    },
    api = {
        usersAndGroups: "selectUserOrGroup"
    },
    init = function (context) {
        $(".stop-inheriting", context.wrapper).click(function (e) {
            e.preventDefault();
            if (window.confirm(context.stopInheritingConfirmMsg)) {
                updatePermissions(context, { 
                    method: "stop-inheriting" 
                }, function(){
                    $(".inherited", context.wrapper).hide();
                    $(".non-inherited", context.wrapper).show();
                });
            }
        });

        $(".start-inheriting", context.wrapper).click(function (e) {
            e.preventDefault();
            updatePermissions(context, {
                method: "start-inheriting"
            }, function(){
                $(".inherited", context.wrapper).show();
                $(".non-inherited", context.wrapper).hide();
            });
        });

        $(".remove-permissions", context.wrapper).click(function (e) {
            e.preventDefault();
            var memberIds = getUserGroupIds();
            if (memberIds.length > 0) {
                updatePermissions(context, {
                    method: "remove",
                    memberIds: memberIds.join(',')
                });
                context.page = 0;
            }
        });

        $(document).bind(eventNames.grantPermissions, function (e, grantPermissionsArgs) {
            if(grantPermissionsArgs){
                clearData();
                refreshPermissionsList(context);
            }
        }).bind(eventNames.editPermissions, function (e, editPermissionsArgs) {
            if(editPermissionsArgs){
                clearData();
                refreshPermissionsList(context);
            }
        });
    },
    userList = [],
    markUserAsSelected = function (id, name, checked, levels, context) {
        userList[id] = { 
            checked: checked,
            name: name,
            levels: levels
        };
        afterUserHasBeenSelected(context);
    },
    afterUserHasBeenSelected = function(context){
        updateNavigationHeader();
        selectedUsersOrGroupsChange(context);
    },
    getUserIds = function () {
        var userArr = [];
        for (var userId in userList) {
            if (userList[userId].checked) {
                userArr.push(userId);
            }
        }
        return userArr.join(",");
    },
    getUserNames = function () {
        var userArr = [];
        for (var userId in userList) {
            if (userList[userId].checked) {
                userArr.push(userList[userId].name);
            }
        }
        return userArr.join(",");
    };

    // Selected groups
    var groupList = [],
    markGroupAsSelected = function (id, name, checked, levels, context) {
        groupList[id] = {
            checked: checked,
            name: name,
            levels: levels
        };
        afterGroupHasBeenSelected(context);
    },
    afterGroupHasBeenSelected = function(context){
        updateNavigationHeader();
        selectedUsersOrGroupsChange(context);
    },
    getGroupIds = function () {
        var groupArr = [];
        for (groupId in groupList) {
            if (groupList[groupId].checked) {
                groupArr.push(groupId);
            }
        }
        return groupArr.join(",");
    },
    getUserGroupIds = function () {
        var memberArr = [];
        for (var userId in userList) {
            if (userList[userId].checked) {
                memberArr.push(userId);
            }
        }
        for (var groupId in groupList) {
            if (groupList[groupId].checked) {
                memberArr.push(groupId);
            }
        }
        return memberArr;
    },
    clearData = function (context) {
        userList.length = 0;
        groupList.length = 0;
        updateNavigationHeader();
        selectedUsersOrGroupsChange(context);
    };

    var refreshPermissionsList = function (context) {
        var data = {};
        if (typeof context.page !== "undefined") {
            data.page = context.page;
        }

        $(context.pagedListHolderId).addClass("disabled");
        $.telligent.evolution.get({
            url: context.permissionsListUrl,
            data: data,
            success: function (response) {
                $(context.pagedListHolderId).html(response).removeClass("disabled");
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
                $(context.pagedListHolderId).removeClass("disabled");
            },
            complete: function () {
                attachHandlers(context);
            }
        });
    },
    updatePermissions = function (context, data, completedEventHandler) {
        $.telligent.evolution.put({
            url: context.updatePermissionsUrl,
            data: data,
            success: function (responseObj) {
                if(responseObj && responseObj.valid){
                    clearData();
                    refreshPermissionsList(context);
                    if (typeof completedEventHandler === 'function') {
                        completedEventHandler();
                    }
                }
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    updateNavigationHeader = function () {
        var memberIds = getUserGroupIds();
        if (memberIds.length > 0) {
            $(".remove-permissions").removeClass("disabled");
            $(".edit-permissions").removeClass("disabled");
        }
        else {
            $(".remove-permissions").addClass("disabled");
            $(".edit-permissions").addClass("disabled");
        }
    },
    selectedUsersOrGroupsChange = function (context) {
        $(document).trigger(api.usersAndGroups, [userList, groupList]);
    },
    attachHandlers = function (context) {

        $(".permissions-footer .pager a", context.pagedListHolderId).click(function (e) {
            e.preventDefault();
            e.stopPropagation();
            var m = this.href.match(/page=(\d+)/);
            if (m && m.length > 1){
                context.page = m[1];
            }
            refreshPermissionsList(context);
        });

        $("tr", context.pagedListHolderId).hover(function () {
            $(this).addClass('hover').find("td.item-checker :checkbox").removeClass("invisible");
        }, function () {
            $(this).removeClass('hover').find("td.item-checker :checkbox:not(:checked)").addClass("invisible");
        });

        $("tr", context.pagedListHolderId).click(function (e) {
            if (e.target.nodeName != "INPUT") {
                $(this).find("td.item-checker :checkbox").each(function () {
                    this.checked = !this.checked;
                    var levels = [];
                    $(this).closest("tr").find("span[levelId]").each(function () {
                        levels.push($(this).attr("levelId"));
                    });

                    if ($(this).attr("isuser") == "true") {
                        markUserAsSelected($(this).attr("id"), $(this).attr("name"), this.checked, levels);
                    } else {
                        markGroupAsSelected($(this).attr("id"), $(this).attr("name"), this.checked, levels);
                    }
                });
            }
        });

        // Recover checkbox states for selected users
        $('td.item-checker :checkbox[isuser="true"]', context.pagedListHolderId).each(function () {
            var userId = $(this).attr("id");
            if (userList[userId] != undefined && userList[userId].checked == true) {
                this.checked = true;
                $(this).removeClass("invisible");
            } else {
                $(this).addClass("invisible");
            }
        });

        $('td.item-checker :checkbox[isuser="true"]', context.pagedListHolderId).change(function () {
            var levels = [];
            $(this).closest("tr").find("span[levelId]").each(function () {
                levels.push($(this).attr("levelId"));
            });
            markUserAsSelected($(this).attr("id"), $(this).attr("name"), this.checked, levels);
        });

        // Recover checkbox states for selected groups
        $('td.item-checker :checkbox[isuser="false"]', context.pagedListHolderId).each(function () {
            var group = $(this).attr("id");
            if (groupList[group] != undefined && groupList[group].checked == true) {
                this.checked = true;
                $(this).removeClass("invisible");
            }
            else {
                $(this).addClass("invisible");
            }
        });

        $('td.item-checker :checkbox[isuser="false"]', context.pagedListHolderId).change(function () {
            var levels = [];
            $(this).closest("tr").find("span[levelId]").each(function () {
                levels.push($(this).attr("levelId"));
            });
            markGroupAsSelected($(this).attr("id"), $(this).attr("name"), this.checked, levels);
        });
    };

    $.telligent.sharepoint.widgets.documentLibrary.listPermissions = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            refreshPermissionsList(context);
            updateNavigationHeader();
        }
    };

})(jQuery, window);

//$.telligent.sharepoint.widgets.documentLibrary.grantPermissions
(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        grantHolderId: '',
        usersGroupsHolderId: '',
        groupHolderId: '',
        webUrl: null,
        updatePermissionsUrl: null
    },
    api = {
        grantPermissions: "grant-permissions"
    },
    grantHtml,
    spinner = '',
    attachHandlers = function (context) {
        $(".submit-button.save", context.grantHolderId).click(function (e) {
            e.preventDefault();
            var grantPermissionsArgs = getGrantPermissionsArgs(context);
            if (grantPermissionsArgs) {
                updatePermissions(context, grantPermissionsArgs, function(){
                    $(document).trigger(api.grantPermissions, grantPermissionsArgs);
                    updateMarkupToDefault(context);
                });
            }
        });
    },
    getGrantPermissionsArgs = function (context) {
        var users = [];
        var groups = [];
        for (var i = 0, len = $(context.usersGroupsHolderId).glowLookUpTextBox('count'); i < len; i++) {
            var userOrGroup = $(context.usersGroupsHolderId).glowLookUpTextBox('getByIndex', i).Value;
            if (userOrGroup.IsGroup) {
                groups.push(userOrGroup);
            } else {
                users.push(userOrGroup);
            }
        }

        // Add users to the group
        var $selected = $("input[name='grant-selector']:checked", context.grantHolderId);
        var operation = $selected.val();
        if (operation == "add-to-group" && $(context.groupHolderId).glowLookUpTextBox('count') > 0) {
            var groupId = $(context.groupHolderId).glowLookUpTextBox('getByIndex', 0).Value.Id;
            if (typeof groupId != 'undefined') {
                return {
                    method: "add-user-to-group",
                    userNames: $.map(users, function (user) { 
                        return user.Name; 
                    }).join(','),
                    groupId: groupId
                };
            }
        }
        else if (operation == "grant-permissions-directly") {
            var levelIds = [];
            $selected.parent().find("input:checkbox:checked[levelId]").each(function () {
                levelIds.push($(this).attr('levelId'));
            });

            return {
                method: "update",
                userNames: $.map(users, function (user) {
                    return user.Name;
                }).join(","),
                groupIds: $.map(groups, function (group) {
                    return group.Id;
                }).join(","),
                levelIds: levelIds.join(","),
                isGranted: true
            };
        }
        return false;
    },
    updateMarkupToDefault = function (context) {
        var defaultHtml = grantHtml.html();
        $(".content", context.grantHolderId).html(defaultHtml);

        // select first radio-button
        $("input[name='grant-selector']", context.grantHolderId).first().click();

        initUserOrGroupTextbox(context, $(context.usersGroupsHolderId));
        initGroupTextbox(context, $(context.groupHolderId));
    },
    updatePermissions = function (context, data, completedEventHandler) {
        $.telligent.evolution.put({
            url: context.updatePermissionsUrl,
            data: data,
            success: function (responseObj) {
                if(responseObj && responseObj.valid){
                    if (typeof completedEventHandler === 'function') {
                        completedEventHandler();
                    }
                    return responseObj;
                }
                return false;
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    initUserOrGroupTextbox = function (context, textbox) {
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: false,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                searchUserOrGroup(context, tb, searchText, false);
            },
            emptyHtml: '',
            selectedLookUpsHtml: '',
            deleteImageUrl: ''
        });

        $(context.usersGroupsCheckerId).hide();
        textbox.bind('glowLookUpTextBoxChange', function () {
            if (textbox.glowLookUpTextBox('count') > 0) {
                $(context.usersGroupsCheckerId).show();
            } else {
                $(context.usersGroupsCheckerId).hide();
            }
        });
    },
    initGroupTextbox = function (context, textbox) {
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: false,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                searchUserOrGroup(context, tb, searchText, true);
            },
            emptyHtml: '',
            selectedLookUpsHtml: '',
            deleteImageUrl: ''
        });
    },
    searchUserOrGroup = function (context, textbox, searchText, onlyGroups) {
        if (searchText && searchText.length > 0) {
            searchText = searchText.trim();
            textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            $.telligent.evolution.get({
                url: (onlyGroups) ? '/api.ashx/v2/sharepoint/groups.json' : '/api.ashx/v2/sharepoint/usersandgroups.json',
                data: {
                    url: context.webUrl,
                    search: searchText
                },
                success: function (response) {
                    var lookUpSuggestions = [];
                    for (var i = 0, len = response.List.length; i < len; i++) {
                        var userOrGroup = response.List[i];
                        if (!onlyGroups || (onlyGroups && userOrGroup.IsGroup)) {
                            var markup = [userOrGroup.Title, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px; color: #777;' title='", userOrGroup.Name, "'>", userOrGroup.Email, "</div>"].join("");
                            lookUpSuggestions.push(textbox.glowLookUpTextBox('createLookUp', userOrGroup, userOrGroup.Name, markup, true));
                        }
                    }
                    textbox.glowLookUpTextBox('updateSuggestions', lookUpSuggestions);
                }
            });
        }
    };

    $.telligent.sharepoint.widgets.documentLibrary.grantPermissions = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            initUserOrGroupTextbox(context, $(context.usersGroupsHolderId));
            initGroupTextbox(context, $(context.groupHolderId));
            attachHandlers(context);
            grantHtml = $(".content", context.grantHolderId).clone();
        }
    };

})(jQuery, window);

//$.telligent.sharepoint.widgets.documentLibrary.editPermissions
(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        holderId: '',
        updatePermissionsUrl: null
    };

    var api = {
        editPermissions: 'edit-permissions'
    },
    eventNames = {
        usersAndGroupsSelected: 'selectUserOrGroup'
    };

    var userList, groupList;
    var attachHandlers = function (context) {
        $(".submit-button.save", context.holderId).click(function (e) {
            e.preventDefault();
            var editPermissionsArgs = getEditPermissionsArgs(context);
            if (editPermissionsArgs) {
                updatePermissions(context, editPermissionsArgs, function(){
                    $(document).trigger(api.editPermissions, editPermissionsArgs);
                });
            }
        });

        $(document).bind(eventNames.usersAndGroupsSelected, function (e, userListArg, groupListArg) {
            userList = userListArg;
            groupList = groupListArg;
            updateUI($(context.holderId));
        });
    },
    getEditPermissionsArgs = function (context) {
        var levelIds = [];
        $("input:checkbox:checked", context.holderId).each(function () {
            levelIds.push($(this).attr('levelId'));
            this.checked = false;
        });

        return {
            method: "update",
            userNames: getUserNames(),
            groupIds: getGroups(),
            levelIds: levelIds.join(",")
        };
    },
    updatePermissions = function (context, data, completedEventHandler) {
        $.telligent.evolution.put({
            url: context.updatePermissionsUrl,
            data: data,
            success: function (responseObj) {
                if(responseObj && responseObj.valid){
                    if (typeof completedEventHandler === 'function') {
                        completedEventHandler();
                    }
                    return responseObj;
                }
                return false;
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    getUserNames = function () {
        var userArr = [];
        for (userId in userList) {
            if (userList[userId].checked) {
                userArr.push(userList[userId].name);
            }
        }
        return userArr.join(",");
    },
    getGroups = function () {
        var groupArr = [];
        for (groupId in groupList) {
            if (groupList[groupId].checked) {
                groupArr.push(groupId);
            }
        }
        return groupArr.join(",");
    },
    updateUI = function ($holder) {
        var memberArr = [];

        // find selected users
        var levels = [];
        for (userId in userList) {
            if (userList[userId] != undefined && userList[userId].checked) {
                memberArr.push(userList[userId].name);
                levels = userList[userId].levels;
            }
        }
        // find selected groups
        for (groupId in groupList) {
            if (groupList[groupId] != undefined && groupList[groupId].checked) {
                memberArr.push(groupList[groupId].name);
                levels = groupList[groupId].levels;
            }
        }
        $holder.find("#selected-users-or-groups").html(memberArr.join(","));
        $holder.find("input:checkbox").each(function () {
            this.checked = false;
        });

        // if only 1 user or group was selected
        if (memberArr.length == 1) {
            $holder.find("input:checkbox").each(function () {
                var levelId = $(this).attr("levelId");
                for (var i = 0; i < levels.length; i++) {
                    if (levels[i] == levelId) {
                        this.checked = true;
                        break;
                    }
                }
            });
        }
    };
    
    $.telligent.sharepoint.widgets.documentLibrary.editPermissions = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };

})(jQuery, window);
