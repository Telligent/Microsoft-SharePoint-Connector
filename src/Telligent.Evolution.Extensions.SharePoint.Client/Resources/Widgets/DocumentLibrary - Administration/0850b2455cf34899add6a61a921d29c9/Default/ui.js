(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    String.prototype.updateQueryString = function (key, value) {
        var uri = this;
        var keyPattern = new RegExp("([?|&])" + key + "=.*?(&|$)", "i");
        var separator = uri.indexOf('?') === -1 ? "?" : "&";
        if (uri.match(keyPattern)) {
            return uri.replace(keyPattern, ['$1', key, "=", value, '$2'].join(''));
        }
        else {
            return [uri, separator, key, "=", value].join('');
        }
    };

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
    eventNames = {
        added: 'documentLibraryAdded',
        removed: 'documentLibraryRemoved'
    },
    successHtml = '<div class="message success">{Message}</div>',
    failHtml = '<div class="message error">{Message}</div>',
    warningHtml = '<div class="message warning">{Message}</div>';

    var attachHandlers = function (context) {
        $('.add-library', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.addModalUrl == null) return false;

            $.glowModal(context.addModalUrl, {
                width: context.addModalWidth,
                height: context.addModalHeight,
                onClose: function (responseObj) {
                    if (responseObj && responseObj.valid) {
                        showSuccessMessage(context.statusWrapper, context.addSuccessMsg.replace(/{LibraryName}/g, responseObj.library.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(global).trigger(eventNames.added, responseObj || {});
                        if (responseObj.urlRedirect && responseObj.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = responseObj.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (responseObj) {
                        showFailMessage(context.statusWrapper, responseObj, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
        });

        $('.create-library', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.createModalUrl == null) return false;

            $.glowModal(context.createModalUrl, {
                width: context.createModalWidth,
                height: context.createModalHeight,
                onClose: function (responseObj) {
                    if (responseObj && responseObj.valid) {
                        showSuccessMessage(context.statusWrapper, context.createSuccessMsg.replace(/{LibraryName}/g, responseObj.library.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(global).trigger(eventNames.added, responseObj || {});
                        if (responseObj.urlRedirect && responseObj.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = responseObj.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (responseObj) {
                        showFailMessage(context.statusWrapper, responseObj, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
        });

        $('.remove-library', context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.removeModalUrl == null) return false;

            $.glowModal(context.removeModalUrl, {
                width: context.removeModalWidth,
                height: context.removeModalHeight,
                onClose: function (responseObj) {
                    if (responseObj && responseObj.valid) {
                        showSuccessMessage(context.statusWrapper, context.removeSuccessMsg.replace(/{LibraryName}/g, responseObj.library.name), context.sliderTimeout, context.statusMessageTimeout);
                        $(global).trigger(eventNames.removed, responseObj || {});
                        if (responseObj.urlRedirect && responseObj.urlRedirect.length > 0) {
                            setTimeout(function () {
                                window.location = responseObj.urlRedirect;
                            }, context.statusMessageTimeout + context.sliderTimeout);
                        }
                    }
                    else if (responseObj) {
                        showFailMessage(context.statusWrapper, responseObj, context.sliderTimeout, context.statusMessageTimeout);
                    }
                }
            });
        });
    },
    showSuccessMessage = function (wrapper, msg, sliderTimeout, statusMessageTimeout) {
        var html = successHtml.replace(/{Message}/g, msg);
        wrapper = wrapper.hide().html(html);
        showMsg(wrapper, sliderTimeout);
        setTimeout(function () {
            hideMsg(wrapper, sliderTimeout);
        }, statusMessageTimeout);
    },
    showFailMessage = function (wrapper, msg, sliderTimeout, statusMessageTimeout) {
        var html = failHtml.replace(/{Message}/g, msg);
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

    $.telligent.sharepoint.widgets.documentLibrariesAdministration = {
        register: function (context) {
            attachHandlers($.extend({}, defaultOptions, context || {}));
        }
    };
})(jQuery, window);


(function($, global){

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrariesAdministration = $.telligent.sharepoint.widgets.documentLibrariesAdministration || {};

    var spinner = ['<div style="text-align: center;"><img src="', $.telligent.evolution.site.getBaseUrl(), 'Utility/spinner.gif" /></div>'].join('');

    // SharePoint Web Url
    var initializeWebUrlControl = function (textbox, startValue, loadingHtml) {
        var context = {
            spinner: loadingHtml
        };

        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPWebs(tb, searchText, context);
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });

        var lookingForSPWebs = function (tb, searchText, ctx) {
            var hasSearchText = searchText && searchText.length >= 1;
            if (hasSearchText) {
                loadSPWebs(tb, searchText, ctx.spinner);
            }
        },
        loadSPWebs = function (tb, searchText, spinner) {
            $.telligent.evolution.get({
                url: '/api.ashx/v2/sharepoint/integration/managers.json',
                data: {
                    SiteNameFilter: searchText,
                    PageSize: 10
                },
                success: function (response) {
                    updateWebUrlControl(tb, response.IntegrationManagerList.Items, spinner);
                },
                complete: function () {
                }
            });
        },
        updateWebUrlControl = function (tb, webItemsCollection, spinner) {
            textbox.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(webItemsCollection, function (webItem, i) {
                    var markup = [webItem.SPSiteName, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px;color: #777;'>", webItem.SPSiteUrl, "</div>"].join('');
                    return tb.glowLookUpTextBox('createLookUp', webItem.SPSiteUrl, webItem.SPSiteName, markup, true);
                })
            );
        };
    };

    // SharePoint Libraries
    var initializeLibrariesControl = function (textbox, startValue, weburlTextbox, spinner) {
        var context = {
            weburlTextbox: weburlTextbox,
            spinner: spinner
        };
        textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPLibraries(tb, searchText, context);
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });

        var lookingForSPLibraries = function (tb, searchText, context) {
            var hasUrl = context.weburlTextbox.val() != '';
            var hasSearchText = searchText && searchText.length >= 1;
            if (hasUrl && hasSearchText) {
                loadSPLibraries(tb, searchText, context.weburlTextbox, context.spinner);
            }
        },
        loadSPLibraries = function (tb, searchText, weburlTextbox, spinner) {
            var url = weburlTextbox.val();
            $.telligent.evolution.get({
                url: '/api.ashx/v2/sharepoint/lists.json',
                data: {
                    url: url,
                    listType: 'DocumentLibrary',
                    ListNameFilter: searchText
                },
                success: function (response) {
                    updateSPLibrariesControl(tb, response.Lists.Items, spinner);
                },
                complete: function () {
                }
            });
        },
        updateSPLibrariesControl = function (tb, listCollection, spinner) {
            textbox.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(listCollection, function (library, i) {
                    var markup = library.Title;
                    return tb.glowLookUpTextBox('createLookUp', library.Id, library.Title, markup, true);
                })
            );
        };
    };

    $.telligent.sharepoint.widgets.documentLibrariesAdministration.addModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper         : document,
                groupId         : -1,
                addLibraryUrl   : '',
                saveButtonId    : null,
                cancelButtonId  : null,
                webUrlTextbox   : null,
                libraryIdTextbox: null
            },
            attachHandlers = function (context) {
                context.webUrlTextbox.bind('glowLookUpTextBoxChange', function () {
                    var disabled = context.webUrlTextbox.val() == '';
                    context.libraryIdTextbox.glowLookUpTextBox('removeByIndex', 0);
                    context.libraryIdTextbox.glowLookUpTextBox('disabled', disabled);
                });

                context.libraryIdTextbox.bind('glowLookUpTextBoxChange', function () {
                    var saveDisabled = context.libraryIdTextbox.val() == '';
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
                    save(context.addListUrl, context.groupId, context.webUrlTextbox.val(), context.libraryIdTextbox.val());
                    return false;
                });

                $(context.cancelButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                });
            },
            save = function (addurl, groupId, spwebUrl, libraryId) {
                $.telligent.evolution.put({
                    url: addurl,
                    data: {
                        groupId  : groupId,
                        spwebUrl : spwebUrl,
                        libraryId: libraryId
                    },
                    success: function (responseObj) {
                        if (responseObj && responseObj.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(responseObj);
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
            initializeLibrariesControl(options.libraryIdTextbox, '', options.webUrlTextbox, spinner);
            attachHandlers(options);
        }
    };

    $.telligent.sharepoint.widgets.documentLibrariesAdministration.createModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper           : document,
                createLibraryUrl  : '',
                groupId           : -1,
                saveButtonId      : null,
                cancelButtonId    : null,
                webUrlTextbox     : null,
                libraryIdTextbox  : null,
                descriptionTextbox: null
            },
            attachHandlers = function (context) {
                context.webUrlTextbox.bind('glowLookUpTextBoxChange', function () {
                    var saveDisabled = context.webUrlTextbox.val() == '' || context.libraryIdTextbox.val() == '';
                    if (saveDisabled) {
                        $(context.saveButtonId, context.wrapper).addClass("disabled");
                    }
                    else {
                        $(context.saveButtonId, context.wrapper).removeClass("disabled");
                    }
                });

                context.libraryIdTextbox.live('input', function () {
                    var saveDisabled = context.webUrlTextbox.val() == '' || context.libraryIdTextbox.val() == '';
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
                    $(this).addClass("disabled");
                    save(context.createListUrl, context.groupId, context.webUrlTextbox.val(), context.libraryIdTextbox.val(), context.descriptionTextbox.val(), function () {
                        $(this).removeClass("disabled");
                    });
                    return false;
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
                    success: function (responseObj) {
                        if (responseObj.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(responseObj);
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

    $.telligent.sharepoint.widgets.documentLibrariesAdministration.removeModal = {
        register: function (context) {
            var defaultOptions = {
                wrapper         : document,
                libraryId       : null,
                removeLibraryUrl   : null,
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
                    save(context.removeLibraryUrl, context.libraryId, $(context.deleteCheckBoxId, context.wrapper).is(':checked'));
                    return false;
                });

                $(context.cancelButtonId, context.wrapper).click(function (e) {
                    e.preventDefault();
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                });
            },
            save = function (removeurl, libraryId, deleteList) {
                $(context.saveButtonId, context.wrapper).addClass("disabled");
                $.telligent.evolution.del({
                    url: removeurl,
                    data: {
                        libraryId: libraryId,
                        deleteList: deleteList
                    },
                    success: function (responseObj) {
                        if (responseObj.valid) {
                            window.parent.$.glowModal.opener(window).$.glowModal.close(responseObj);
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