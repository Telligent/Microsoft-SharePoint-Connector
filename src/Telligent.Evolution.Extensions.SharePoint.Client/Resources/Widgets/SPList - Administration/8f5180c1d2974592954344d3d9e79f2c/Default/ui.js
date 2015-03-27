(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        statusWrapper: null,

        addModalWidth: 500,
        addModalHeight: 300,
        addModalUrl: null,
        addSuccessMsg: '',

        createModalWidth: 500,
        createModalHeight: 300,
        createModalUrl: null,
        createSuccessMsg: '',

        removeModalWidth: 500,
        removeModalHeight: 300,
        removeModalUrl: null,
        removeSuccessMsg: '',

        container: document,
        sliderTimeout: 500,
        statusMessageTimeout: 5000
    },
    _eventNames = {
        added: 'sharepointListAdded',
        removed: 'sharepointListRemoved'
    },
    _successHtml = '<div class="message success">{Message}</div>',
    _failHtml = '<div class="message error">{Message}</div>',
    _warningHtml = '<div class="message warning">{Message}</div>';

    var attachHandlers = function (context) {
        $('.add-list', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.addModalUrl == null) return false;

            $.glowModal(context.addModalUrl, {
                width: context.addModalWidth,
                height: context.addModalHeight,
                onClose: function (response) {
                    if (response && response.valid) {
                        showSuccessMessage(context.statusWrapper, context.addSuccessMsg.replace(/{ListName}/g, response.list.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(global).trigger(_eventNames.added, response || {});
                        if (response.urlRedirect && response.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = response.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (response) {
                        showFailMessage(context.statusWrapper, response, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
            return false;
        });

        $('.create-list', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.createModalUrl == null) return false;

            $.glowModal(context.createModalUrl, {
                width: context.createModalWidth,
                height: context.createModalHeight,
                onClose: function (response) {
                    if (response && response.valid) {
                        showSuccessMessage(context.statusWrapper, context.createSuccessMsg.replace(/{ListName}/g, response.list.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(document).trigger(_eventNames.added, response || {});
                        if (response.urlRedirect && response.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = response.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (response) {
                        showFailMessage(context.statusWrapper, response, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
            return false;
        });

        $('.remove-list', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.removeModalUrl == null) return false;

            $.glowModal(context.removeModalUrl, {
                width: context.removeModalWidth,
                height: context.removeModalHeight,
                onClose: function (response) {
                    if (response && response.valid) {
                        showSuccessMessage(context.statusWrapper, context.removeSuccessMsg.replace(/{ListName}/g, response.list.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(document).trigger(_eventNames.removed, response || {});
                        if (response.urlRedirect && response.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = response.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (response) {
                        showFailMessage(context.statusWrapper, response, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
            return false;
        });
    },
    showSuccessMessage = function (wrapper, msg, sliderTimeout, statusMessageTimeout) {
        var html = _successHtml.replace(/{Message}/g, msg);
        wrapper = wrapper.hide().html(html);
        showMsg(wrapper, sliderTimeout);
        setTimeout(function () {
            hideMsg(wrapper, sliderTimeout);
        }, statusMessageTimeout);
    },
    showFailMessage = function (wrapper, msg, sliderTimeout, statusMessageTimeout) {
        var html = _failHtml.replace(/{Message}/g, msg);
        wrapper = wrapper.hide().html(html);
        showMsg(wrapper, sliderTimeout);
        setTimeout(function () {
            hideMsg(wrapper, sliderTimeout);
        }, statusMessageTimeout);
    },
    showMsg = function (wrapper, sliderTimeout) {
        wrapper.css("visibility", "visible").slideDown(sliderTimeout);
    },
    hideMsg = function (wrapper, sliderTimeout) {
        wrapper.slideUp(sliderTimeout, function () {
            $(this).css("visibility", "hidden");
        });
    };

    $.telligent.sharepoint.widgets.listsAdministration = {
        register: function (context) {
            attachHandlers($.extend({}, defaultOptions, context || {}));
        }
    };
})(jQuery, window);

(function($, global){

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.listsAdministration = $.telligent.sharepoint.widgets.listsAdministration || {};

    var spinner = ['<div style="text-align: center;"><img src="', $.telligent.evolution.site.getBaseUrl(), 'Utility/spinner.gif" /></div>'].join('');

    // SharePoint Web Url
    var initializeWebUrlControl = function (textbox, startValue, spinner) {
        var context = {
            spinner: spinner
        };
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPWebs(tb, searchText, context)
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });

        var lookingForSPWebs = function (textbox, searchText, context) {
            var hasSearchText = searchText && searchText.length >= 1;
            if (hasSearchText) {
                loadSPWebs(textbox, searchText, context.spinner);
            }
        },

        loadSPWebs = function (textbox, searchText, spinner) {
            $.telligent.evolution.get({
                url: '/api.ashx/v2/sharepoint/integration/managers.json',
                data: {
                    SiteNameFilter: searchText,
                    PageSize: 10
                },
                success: function (response) {
                    updateWebUrlControl(textbox, response.IntegrationManagerList.Items, spinner);
                },
                complete: function () {
                }
            });
        },

        updateWebUrlControl = function (textbox, webItemsCollection, spinner) {
            textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(webItemsCollection, function (webItem, i) {
                    var markup = [webItem.SPSiteName, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px;color: #777;'>", webItem.SPSiteUrl, "</div>"].join('');
                    return textbox.glowLookUpTextBox('createLookUp', webItem.SPSiteUrl, webItem.SPSiteName, markup, true);
                })
            );
        };
    };

    // SharePoint Lists
    var initializeListsControl = function (textbox, startValue, weburlTextbox, spinner) {
        var context = {
            weburlTextbox: weburlTextbox,
            spinner: spinner
        };
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPLists(tb, searchText, context)
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });

        var lookingForSPLists = function (textbox, searchText, context) {
            var hasUrl = context.weburlTextbox.val() != '';
            var hasSearchText = searchText && searchText.length >= 1;
            if (hasUrl && hasSearchText) {
                loadSPLists(textbox, searchText, context.weburlTextbox, context.spinner);
            }
        },

        loadSPLists = function (textbox, searchText, weburlTextbox, spinner) {
            var url = weburlTextbox.val();
            $.telligent.evolution.get({
                url: '/api.ashx/v2/sharepoint/lists.json',
                data: {
                    url: url,
                    ExcludeListType: "DocumentLibrary",
                    ListNameFilter: searchText
                },
                success: function (response) {
                    updateSPListsControl(textbox, response.Lists.Items, spinner);
                },
                complete: function () {
                }
            });
        },

        updateSPListsControl = function (textbox, listCollection, spinner) {
            textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(listCollection, function (list, i) {
                    var markup = list.Title;
                    return textbox.glowLookUpTextBox('createLookUp', list.Id, list.Title, markup, true);
                })
            );
        };
    };

    $.telligent.sharepoint.widgets.listsAdministration.addModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper        : document,
                groupId        : -1,
                addListUrl     : '',
                saveButtonId   : null,
                cancelButtonId : null,
                webUrlTextbox  : null,
                listIdTextbox  : null
            },
            attachHandlers = function (context) {
                context.webUrlTextbox.bind('glowLookUpTextBoxChange', function () {
                    var disabled = context.webUrlTextbox.val() == '';
                    context.listIdTextbox.glowLookUpTextBox('removeByIndex', 0);
                    context.listIdTextbox.glowLookUpTextBox('disabled', disabled)
                });

                context.listIdTextbox.bind('glowLookUpTextBoxChange', function () {
                    var saveDisabled = context.listIdTextbox.val() == '';
                    if (saveDisabled) {
                        $(context.saveButtonId, context.wrapper).addClass("disabled");
                    }
                    else {
                        $(context.saveButtonId, context.wrapper).removeClass("disabled");
                    }
                });

                $(context.saveButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    if ($(this).hasClass('disabled')) {
                        return false;
                    }
                    save(context.addListUrl, context.groupId, context.webUrlTextbox.val(), context.listIdTextbox.val());
                });

                $(context.cancelButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                });
            },
            save = function (addurl, groupId, spwebUrl, listId) {
                $.telligent.evolution.put({
                    url: addurl,
                    data: {
                        groupId  : groupId,
                        spwebUrl : spwebUrl,
                        listId   : listId
                    },
                    success: function (response) {
                        if (response && response.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(response);
                        }
                    },
                    error: function (xhr, textStatus, errorThrown) {
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
            };

            var options = $.extend({}, defaultOptions, context || {});
            initializeWebUrlControl(options.webUrlTextbox, '', spinner);
            initializeListsControl(options.listIdTextbox, '', options.webUrlTextbox, spinner);
            attachHandlers(options);
        }
    };

    $.telligent.sharepoint.widgets.listsAdministration.createModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper        : document,
                createListUrl  : '',
                groupId        : -1,
                saveButtonId   : null,
                cancelButtonId : null,
                webUrlTextbox  : null,
                listIdTextbox  : null,
                descriptionTextbox: null
            },
            attachHandlers = function (context) {
                context.webUrlTextbox.bind('glowLookUpTextBoxChange', function () {
                    var saveDisabled = context.webUrlTextbox.val() == '' || context.listIdTextbox.val() == '';
                    if (saveDisabled) {
                        $(context.saveButtonId, context.wrapper).addClass("disabled");
                    }
                    else {
                        $(context.saveButtonId, context.wrapper).removeClass("disabled");
                    }
                });

                context.listIdTextbox.live('input', function () {
                    var saveDisabled = context.webUrlTextbox.val() == '' || context.listIdTextbox.val() == '';
                    if (saveDisabled) {
                        $(context.saveButtonId, context.wrapper).addClass("disabled");
                    }
                    else {
                        $(context.saveButtonId, context.wrapper).removeClass("disabled");
                    }
                })

                $(context.saveButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    if ($(this).hasClass('disabled')) {
                        return false;
                    }
                    $(this).addClass("disabled");
                    save(context.createListUrl, context.groupId, context.webUrlTextbox.val(), context.listIdTextbox.val(), context.descriptionTextbox.val(), function () {
                        $(this).removeClass("disabled");
                    });
                });

                $(context.cancelButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                });
            },
            save = function (createurl, groupId, spwebUrl, listName, listDescription, completedCallback) {
                $.telligent.evolution.post({
                    url: createurl,
                    data: {
                        groupId     : groupId,
                        spwebUrl    : spwebUrl,
                        name        : listName,
                        description : listDescription
                    },
                    success: function (response) {
                        if (response.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(response);
                        }
                    },
                    error: function (xhr, textStatus, errorThrown) {
                        var errorMsg;
                        try {
                            var errorHolder = eval('(' + xhr.responseText + ')');
                            errorMsg = errorHolder.Errors[0];
                        }
                        catch (ex) {
                            errorMsg = xhr.responseText;
                        }
                        window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
                    },
                    complete: function () {
                        completedCallback();
                    }
                });
            };

            var options = $.extend({}, defaultOptions, context || {});
            initializeWebUrlControl(options.webUrlTextbox, '', spinner);
            attachHandlers(options);
        }
    };

    $.telligent.sharepoint.widgets.listsAdministration.removeModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper         : document,
                listId          : null,
                removeListUrl   : null,
                deleteCheckBoxId: null,
                saveButtonId    : null,
                cancelButtonId  : null
            },
            attachHandlers = function (context) {
                $(context.saveButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    if ($(this).hasClass('disabled')) {
                        return false;
                    }
                    save(context.removeListUrl, context.listId, $(context.deleteCheckBoxId, context.wrapper).is(':checked'));
                });

                $(context.cancelButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                });
            },
            save = function (removeurl, listId, deleteList) {
                $(context.saveButtonId, context.wrapper).addClass("disabled");
                $.telligent.evolution.del({
                    url: removeurl,
                    data: {
                        listId: listId,
                        deleteList: deleteList
                    },
                    success: function (response) {
                        if (response.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(response);
                        }
                    },
                    error: function (xhr, textStatus, errorThrown) {
                        var errorMsg;
                        try {
                            var errorHolder = eval('(' + xhr.responseText + ')');
                            errorMsg = errorHolder.Errors[0];
                        }
                        catch (ex) {
                            errorMsg = xhr.responseText;
                        }
                        window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
                    },
                    complete: function () {
                        $(context.saveButtonId, context.wrapper).removeClass("disabled");
                    }
                });
            };

            attachHandlers($.extend({}, defaultOptions, context || {}));
        }
    };
})(jQuery, window);