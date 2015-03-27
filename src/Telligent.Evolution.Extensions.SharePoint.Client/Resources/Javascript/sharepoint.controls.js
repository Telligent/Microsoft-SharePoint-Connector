// $.telligent.sharepoint.controls
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};

    var spinner = ['<div style="text-align: center;"><img src="', $.telligent.evolution.site.getBaseUrl(), 'Utility/spinner.gif" /></div>'].join(''),
    // SharePoint Web Url
    initializeWebUrlControl = function (textbox, startValue) {
        var findSPWebs = function (tb, searchText) {
            var search = searchText && searchText.length > 0 ? searchText.replace(/\s+/g, ' ') : '';
            if (search && search.length >= 3) {
                $.telligent.evolution.get({
                    url: '/api.ashx/v2/sharepoint/integration/managers.json',
                    data: {
                        SiteNameFilter: search,
                        PageSize: 10
                    },
                    success: function (response) {
                        updateWebUrlControl(tb, response.IntegrationManagerList.Items);
                    },
                    error: function (xhr, desc, ex) {
                        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                        }
                        else {
                            $.telligent.evolution.notifications.show(desc, { type: 'error' });
                        }
                    }
                });
            }
        },
        updateWebUrlControl = function (tb, webItemsCollection) {
            textbox.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(webItemsCollection, function (webItem, i) {
                    var markup = [webItem.SPSiteName, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px; color: #777;'>", webItem.SPSiteUrl, "</div>"].join('');
                    return tb.glowLookUpTextBox('createLookUp', webItem.SPSiteUrl, webItem.SPSiteName, markup, true);
                })
            );
        };
        return textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: findSPWebs,
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });
    },
    // SharePoint Lists
    initializeListsControl = function (textbox, startValue, get_webUrl, data) {
        if (typeof get_webUrl !== "function") throw "get_webUrl() expected.";

        var findSPLists = function (tb, searchText) {
            var search = searchText && searchText.length > 0 ? searchText.replace(/\s+/g, ' ') : '',
                webUrl = get_webUrl();
            if (search && search.length >= 3 && webUrl.length > 0) {
                data.url = webUrl;
                data.ListNameFilter = search;
                $.telligent.evolution.get({
                    url: '/api.ashx/v2/sharepoint/lists.json',
                    data: data,
                    success: function (response) {
                        updateSPListsControl(tb, response.Lists.Items);
                    },
                    error: function (xhr, desc, ex) {
                        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                        }
                        else {
                            $.telligent.evolution.notifications.show(desc, { type: 'error' });
                        }
                    }
                });
            }
        },
        updateSPListsControl = function (tb, listCollection) {
            textbox.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(listCollection, function (library, i) {
                    var markup = library.Title;
                    return tb.glowLookUpTextBox('createLookUp', library.Id, library.Title, markup, true);
                })
            );
        };
        return textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: findSPLists,
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });
    };
    // SharePoint List Views
    initializeListViewsControl = function (textbox, startValue, get_webUrl, get_listId) {
        if (typeof get_webUrl !== "function") throw "get_webUrl() expected.";
        if (typeof get_listId !== "function") throw "get_listId() expected.";

        var findSPListViews = function (tb, searchText) {
            var search = searchText && searchText.length > 0 ? searchText.replace(/\s+/g, ' ') : '',
                webUrl = get_webUrl(),
                listId = get_listId();
            if (search && search.length >= 3 && webUrl.length > 0 && listId.length > 0) {
                var data = {
                    url: webUrl,
                    listId: listId,
                    ViewNameFilter: search
                };
                $.telligent.evolution.get({
                    url: '/api.ashx/v2/sharepoint/lists/{listId}/views.json',
                    data: data,
                    success: function (response) {
                        updateSPListViewsControl(tb, response.Views.Items);
                    },
                    error: function (xhr, desc, ex) {
                        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                        }
                        else {
                            $.telligent.evolution.notifications.show(desc, { type: 'error' });
                        }
                    }
                });
            }
        },
        updateSPListViewsControl = function (tb, viewCollection) {
            textbox.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            textbox.glowLookUpTextBox('updateSuggestions',
                $.map(viewCollection, function (view, i) {
                    var markup = view.Title;
                    return tb.glowLookUpTextBox('createLookUp', view.Id, view.Title, markup, true);
                })
            );
        };
        return textbox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: findSPListViews,
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });
    };

    $.telligent.sharepoint.controls = {
        /**
         * Initializes and returns a glowLookUpTextbox for SharePoint Web URLs
         * @param {String} textbox Target input selector
         * @param {String} value StartValue
         */
        glowWebUrl: function (options) {
            return initializeWebUrlControl($(options.textbox), options.value);
        },
        /**
         * Initializes and returns a glowLookUpTextbox for SharePoint Libraries
         * @param {String} textbox Target input selector
         * @param {String} value StartValue
         * @param {String} webUrl A constant value of SharePoitn Web URL
         * @param {function} get_webUrl A callback function that returns a SharePoitn Web URL (optional)
         */
        glowLibrary: function (options) {
            if (typeof options.get_webUrl === "undefined") {
                if (options.webUrl && options.webUrl.length > 0) {
                    options.get_webUrl = function () {
                        return options.webUrl;
                    };
                }
                else {
                    throw "options.webUrl value cannot be empty!";
                }
            }
            return initializeListsControl($(options.textbox), options.value, options.get_webUrl, {
                ListType: 'DocumentLibrary'
            });
        },
        /**
        * Initializes and returns a glowLookUpTextbox for SharePoint Lists
        * @param {String} textbox Target input selector
        * @param {String} value StartValue
        * @param {String} webUrl A constant value of SharePoitn Web URL
        * @param {function} get_webUrl A callback function that returns a SharePoitn Web URL (optional)
        */
        glowList: function (options) {
            if (typeof options.get_webUrl === "undefined") {
                if (options.webUrl && options.webUrl.length > 0) {
                    options.get_webUrl = function () {
                        return options.webUrl;
                    };
                }
                else {
                    throw "options.webUrl value cannot be empty!";
                }
            }
            return initializeListsControl($(options.textbox), options.value, options.get_webUrl, {
                ExcludeListType: "DocumentLibrary",
            });
        },
        /**
        * Initializes and returns a glowLookUpTextbox for SharePoint List Views
        * @param {String} textbox Target input selector
        * @param {String} value StartValue
        * @param {String} webUrl A constant value of SharePoint Web URL
        * @param {function} get_webUrl A callback function that returns a SharePoitn Web URL (optional)
        * @param {String} listId A constant value of SharePoint List Id
        * @param {function} get_listId A callback function that returns a SharePoitn List Id (optional)
        */
        glowListView: function (options) {
            if (typeof options.get_webUrl === "undefined") {
                if (options.webUrl && options.webUrl.length > 0) {
                    options.get_webUrl = function () {
                        return options.webUrl;
                    };
                }
            }
            if (typeof options.get_listId === "undefined") {
                if (options.listId && options.listId.length > 0) {
                    options.get_listId = function () {
                        return options.listId;
                    };
                }
            }
            return initializeListViewsControl($(options.textbox), options.value, options.get_webUrl, options.get_listId);
        }
    };
})(jQuery, window);
