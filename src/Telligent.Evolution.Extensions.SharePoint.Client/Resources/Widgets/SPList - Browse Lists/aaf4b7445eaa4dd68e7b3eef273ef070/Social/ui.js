(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        sortBy: null,
        sortOrder: null,
        pagedContent: null,
        pagedContentUrl: null,
        searchResults: null,
        searchResultsUrl: null,
        searchTimeout: 500
    },
    attachHandlers = function (context) {
        $(context.wrapper).on('change', '.sort select', function (e) {
            context.sortBy = $('.sort .by select', context.wrapper).val();
            context.sortOrder = $('.sort .order select', context.wrapper).val();
            refresh($(context.pagedContent, context.wrapper), context.pagedContentUrl, {
                w_sortBy: context.sortBy,
                w_sortOrder: context.sortOrder
            });
        });
        $(context.wrapper).on('keyup', '.search>.field-list>.field-item>.field-item-input>input', function (e) {
            e.preventDefault();
            context._lastKeyCode = e.keyCode;
            if (context._searchTimeout) {
                clearTimeout(context._searchTimeout);
            }
            var searchText = $(this).val();
            context._searchTimeout = setTimeout(function () {
                if (searchText.length > 0) {
                    refresh($(context.searchResults, context.wrapper), context.searchResultsUrl, {
                        w_searchText: searchText
                    });
                }
                else {
                    refresh($(context.pagedContent, context.wrapper), context.pagedContentUrl, {
                        w_sortBy: context.sortBy,
                        w_sortOrder: context.sortOrder
                    });
                }
            }, context.searchTimeout);
        });
    },
    refresh = function (widget, url, data) {
        $.telligent.evolution.get({
            url: url,
            data: data,
            success: function (htmlMarkup) {
                $(widget).html(htmlMarkup);
            },
            cache: false,
            error: function (xhr, desc, ex) {
                if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                    $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                }
                else {
                    $.telligent.evolution.notifications.show(desc, { type: 'error' });
                }
            }
        });
    };

    $.telligent.sharepoint.widgets.browseLists = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);
