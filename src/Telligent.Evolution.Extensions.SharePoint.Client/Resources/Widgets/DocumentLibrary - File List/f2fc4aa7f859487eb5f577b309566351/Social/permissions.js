//$.telligent.sharepoint.widgets.permissionsModal
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        listTab: $('.tab-list .tab-item.permissions-list'),
        editTab: $('.tab-list .tab-item.permissions-edit'),
        grantTab: $('.tab-list .tab-item.permissions-grant'),
        permissionsListUrl: null,
        permissionsUpdateUrl: null,
        pagedListHolder: null,
        stopInheritingConfirmMsg: "You are about to create unique permissions for this document. Changes made to the parent folder or document library permissions will no longer affect this document.",
        startInheritingConfirmMsg: null,
        webUrl: null
    },
    spinner = '',
    api = {
        events: {
            dataChanged: "permissionsDataHasBeenChanged"
        },
        data: {
            users: [],
            groups: []
        },
        clear: function (context) {
            api.data.users.length = 0;
            api.data.groups.length = 0;
            $(global).trigger(api.events.dataChanged);
        },
        refresh: function (context, before, after) {
            if (typeof before === "function") before(context);
            $.telligent.evolution.get({
                url: context.permissionsListUrl,
                data: {
                    page: context.page || 0
                },
                success: function (htmlResponse) {
                    api.clear(context);
                    $(context.pagedListHolder).html(htmlResponse);
                    if (typeof after === "function") after(context, htmlResponse);
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
        update: function (context, data, before, after) {
            if (typeof before === "function") before(context);
            $.telligent.evolution.put({
                url: context.permissionsUpdateUrl,
                data: data,
                success: function (response) {
                    api.data.users.length = 0;
                    api.data.groups.length = 0;
                    if (typeof after === "function") after(context, response);
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
        }
    },
    registerTabs = function (context) {
        var $tabs = $('.tab-list>.tab-item');
        $tabs.hide();
        $(global).bind('hashchange', function (e) {
            var hash = global.location.hash.split('#')[1] || '';
            if (hash.length == 0) {
                $tabs.hide().first().show();
                return false;
            }
            $tabs.each(function () {
                if (this.id === hash) {
                    $(this).show();
                }
                else {
                    $(this).hide();
                }
            });
        });
        $("nav a").click(function (e) {
            return !$(this).hasClass("disabled");
        });
    },
    ListView = {
        show: function (context) {
            window.location.hash = "list";
        },
        register: function (context) {
            var startInheriting = function (context) {
                $(".stop-inheriting", context.listTab).show();
                $(".start-inheriting", context.listTab).hide();
                $(".grant-permissions", context.listTab).hide();
                $(".edit-permissions", context.listTab).hide();
                $(".remove-permissions", context.listTab).hide();
            },
            stopInheriting = function (context) {
                $(".stop-inheriting", context.listTab).hide();
                $(".start-inheriting", context.listTab).show();
                $(".grant-permissions", context.listTab).show();
                $(".edit-permissions", context.listTab).show();
                $(".remove-permissions", context.listTab).show();
            };

            (context.inherited ? startInheriting : stopInheriting)(context);

            $(".stop-inheriting .button", context.listTab).click(function (e) {
                e.preventDefault();
                var that = this;
                if (window.confirm(context.stopInheritingConfirmMsg)) {
                    api.update(context, {
                        method: "stop-inheriting"
                    },
                    function (context) {
                        $(that).addClass('disabled');
                    },
                    function (context) {
                        context.page = 0;
                        api.refresh(context);
                        stopInheriting(context);
                        $(that).removeClass('disabled');
                    });
                }
            });

            $(".start-inheriting .button", context.listTab).click(function (e) {
                e.preventDefault();
                var that = this;
                if (context.startInheritingConfirmMsg == null || window.confirm(context.startInheritingConfirmMsg)) {
                    api.update(context, {
                        method: "start-inheriting"
                    },
                    function (context) {
                        $(that).addClass('disabled');
                    },
                    function (context) {
                        context.page = 0;
                        api.refresh(context);
                        startInheriting(context);
                        $(that).removeClass('disabled');
                    });
                }
            });

            $(".remove-permissions .button", context.listTab).click(function (e) {
                e.preventDefault();
                var that = this;
                var members = api.data.users.concat(api.data.groups);
                if (members.length === 0) return false;

                api.update(context, {
                    method: "remove",
                    memberIds: $.map(members, function (member, i) {
                        return member.id;
                    }).join(',')
                },
                function (context) {
                    $(that).addClass('disabled');
                },
                function (context) {
                    context.page = 0;
                    api.refresh(context);
                    $(that).removeClass('disabled');
                });
            });

            $(context.listTab).on('click', ".pager a", function (e) {
                e.preventDefault();
                e.stopPropagation();
                context.page = +$(this).data('page') - 1;
                api.refresh(context, null, null);
            }).on('click', ".table-item", function (e) {
                var that = this;
                $(".item-checker :checkbox", this).each(function () {
                    if (e.target.nodeName != "INPUT") {
                        this.checked = !this.checked;
                    }
                    var id = $(this).data('id'),
                        login = $(this).data('loginname'),
                        type = $(this).data('type'),
                        array = type === "user" ? api.data.users : api.data.groups;
                    if (this.checked) {
                        var levels = [];
                        $(".level-item", that).each(function () {
                            levels.push($(this).data("id"));
                        });
                        array.push({
                            id: id,
                            login: login,
                            levels: levels
                        });
                    }
                    else {
                        var i = array.length;
                        while (i--) {
                            if (array[i].id == id) array.splice(i, 1);
                        }
                    }
                    $(global).trigger(api.events.dataChanged);
                });
            });

            var enableDisableButtons = function (context) {
                if (api.data.users.length + api.data.groups.length === 0) {
                    $(".edit-permissions .button", context.listTab).addClass('disabled');
                    $(".remove-permissions .button", context.listTab).addClass('disabled');
                } else {
                    $(".edit-permissions .button", context.listTab).removeClass('disabled');
                    $(".remove-permissions .button", context.listTab).removeClass('disabled');
                }
            };

            enableDisableButtons(context);
            $(global).bind(api.events.dataChanged, function () {
                enableDisableButtons(context);
            });
        }
    },
    EditView = {
        show: function (context) {
            window.location.hash = "edit";
        },
        register: function (context) {
            var refresh = function (context) {
                var members = api.data.users.concat(api.data.groups);
                $("#users-and-groups", context.editTab).html(
                    $.map(members, function (member, i) {
                        return member.login;
                    }).join(",")
                );
                $("input:checkbox", context.editTab).each(function () {
                    this.checked = false;
                });
                // if only 1 user or group was selected
                if (members.length == 1) {
                    var levels = members[0].levels;
                    $("input:checkbox", context.editTab).each(function () {
                        var levelId = $(this).data("id");
                        for (var i = 0, len = levels.length; i < len && !this.checked; i++) {
                            if (levels[i] == levelId) {
                                this.checked = true;
                            }
                        }
                    });
                }
            };

            $(".save.button", context.editTab).click(function (e) {
                e.preventDefault();
                var that = this;
                $(that).addClass('disabled');
                api.update(context, {
                    method: "update",
                    userNames: $.map(api.data.users, function (user, i) {
                        return user.login;
                    }).join(","),
                    groupIds: $.map(api.data.groups, function (group, i) {
                        return group.id;
                    }).join(","),
                    levelIds: $.map($("input:checkbox:checked", context.editTab), function (htmlElement, i) {
                        return $(htmlElement).data('id');
                    }).join(",")
                },
                null,
                function (context, response) {
                    $(that).removeClass('disabled');
                    ListView.show(context);
                    context.page = 0;
                    api.refresh(context);
                });
            });

            $(global).bind(api.events.dataChanged, function () {
                refresh(context);
            });
        }
    },
    GrantView = {
        show: function (context) {
            window.location.hash = "grant";
        },
        register: function (context) {
            var initUserOrGroupTextbox = function (context, textbox) {
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
            },
            revertControlsToDefaultValues = function (context) {
                $("input[name='grant-selector']", context.grantTab).first().attr('checked', 'checked');
                $("input:checkbox:checked", context.grantTab).removeAttr('checked');
                $("#UsersOrGroupsHolderId", context.grantTab).glowLookUpTextBox('removeByIndex', 0);
                $('#GroupsHolderId', context.grantTab).glowLookUpTextBox('removeByIndex', 0);
            };

            initUserOrGroupTextbox(context, $("#UsersOrGroupsHolderId", context.grantTab));
            initGroupTextbox(context, $("#GroupsHolderId", context.grantTab));

            $(".save.button", context.grantTab).click(function (e) {
                e.preventDefault();
                var users = [],
                groups = [];
                for (var i = 0, len = $("#UsersOrGroupsHolderId", context.grantTab).glowLookUpTextBox('count') ; i < len; i++) {
                    var userOrGroup = $("#UsersOrGroupsHolderId", context.grantTab).glowLookUpTextBox('getByIndex', i).Value,
                        targetArr = userOrGroup.IsGroup ? groups : users;
                    targetArr.push(userOrGroup);
                }
                var operation = $("input[name='grant-selector']:checked", context.grantTab).val();
                if (operation == "add-to-group" && $('#UsersOrGroupsHolderId', context.grantTab).glowLookUpTextBox('count') > 0) {
                    var groupId = $('#GroupsHolderId', context.grantTab).glowLookUpTextBox('getByIndex', 0).Value.Id;
                    if (typeof groupId !== 'undefined' && users.length > 0) {
                        var that = this;
                        api.update(context, {
                            method: "add-user-to-group",
                            userNames: $.map(users, function (user) {
                                return user.Name;
                            }).join(','),
                            groupId: groupId
                        },
                        function (context) {
                            $(that).addClass('disabled');
                        },
                        function (context, response) {
                            $(that).removeClass('disabled');
                            ListView.show(context);
                            context.page = 0;
                            api.refresh(context);
                            revertControlsToDefaultValues(context);
                            $(global).trigger(api.events.dataChanged);
                        });
                    }
                }
                else if (operation == "grant-permissions-directly" && (users.length > 0 || groups.length > 0)) {
                    var that = this;
                    api.update(context, {
                        method: "update",
                        userNames: $.map(users, function (user) {
                            return user.Name;
                        }).join(','),
                        groupIds: $.map(groups, function (group) {
                            return group.Id;
                        }).join(","),
                        levelIds: $.map($("input:checkbox:checked", context.grantTab), function (checkbox) {
                            return $(checkbox).data('id');
                        }).join(","),
                        isGranted: true
                    },
                    function (context) {
                        $(that).addClass('disabled');
                    },
                    function (context, response) {
                        $(that).removeClass('disabled');
                        ListView.show(context);
                        context.page = 0;
                        api.refresh(context);
                        revertControlsToDefaultValues(context);
                        $(global).trigger(api.events.dataChanged);
                    });
                }
                return false;
            });
        }
    };

    $.telligent.sharepoint.widgets.permissionsModal = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            registerTabs(context);
            ListView.register(context);
            ListView.show(context);
            EditView.register(context);
            GrantView.register(context);
        }
    };
})(jQuery, window);
