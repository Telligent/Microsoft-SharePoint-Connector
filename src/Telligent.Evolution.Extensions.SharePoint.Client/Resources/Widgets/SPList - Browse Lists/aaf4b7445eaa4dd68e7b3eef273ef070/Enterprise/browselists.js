(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        listsUrl: null,
        defaultSearchText: 'Search',
        searchResultsUrl: null
    },
    listsAdministrationEvent = {
        added: 'sharepointListAdded',
        removed: 'sharepointListRemoved'
    },
    init = function (context) {
        $(global).bind(listsAdministrationEvent.added, function (e, listAddedEventArgs) {
            // a new list has been added
            refresh($('.sharepoint-list-paged-list', context.wrapper), context.listsUrl);
        }).bind(listsAdministrationEvent.removed, function (e, listRemovedEventArgs) {
            // a list has been removed
            refresh($('.sharepoint-list-paged-list', context.wrapper), context.listsUrl);
        });
        buildEvolutionSort(context.wrapper, context.listsUrl);
    },
    attachHandlers = function (context) {
        $(context.wrapper).on("click", '.table-header-column', function (e) {
            e.preventDefault();
            if ($('a', this).length > 0) {
                var sortBy = $('a', this).attr('sortBy') || '';
                var sortOrder = $('a', this).attr('sortOrder') || '';
                refresh($('.sharepoint-list-paged-list', context.wrapper), context.listsUrl, {
                    w_sortBy: sortBy,
                    w_sortOrder: sortOrder
                });
            }
        }).on("click", '.field-item-input input', function (e) {
            e.preventDefault();
            if ($(this).val() === context.defaultSearchText) {
                $(this).val('');
            }
        }).on("keyup", '.field-item-input input', function (e) {
            e.preventDefault();
            context._lastKeyCode = e.keyCode;
            if (context._searchTimeout) {
                clearTimeout(context._searchTimeout);
            }
            var searchText = $(this).val();
            if (searchText.length > 0) {
                $('.internal-link.clear-search', context.wrapper).show();
                context._searchTimeout = setTimeout(function () {
                    refresh($('.data-holder', context.wrapper), context.searchResultsUrl, {
                        w_searchText: searchText
                    });
                }, 500);
            }
            else {
                $('.internal-link.clear-search', context.wrapper).hide();
                context._searchTimeout = setTimeout(function () {
                    refresh($('.sharepoint-list-paged-list', context.wrapper), context.listsUrl);
                }, 500);
            }
        }).on("click", '.internal-link.clear-search', function (e) {
            e.preventDefault();
            $('.field-item-input input', context.wrapper).val('');
            refresh($('.sharepoint-list-paged-list', context.wrapper), context.listsUrl);
        });
    },
    buildEvolutionSort = function (wrapper, listsUrl) {
        $(".sort-options", wrapper).evolutionSort().bind('evolutionSort', function (e, sort) {
            refresh(wrapper, listsUrl, {
                w_sortBy: sort.prop,
                w_sortOrder: sort.direction
            });
        });
    },
    refresh = function (wrapper, listsUrl, data) {
        $.telligent.evolution.get({
            url: listsUrl,
            data: data,
            success: function (htmlMarkup) {
                $(wrapper).html(htmlMarkup);
                buildEvolutionSort(wrapper, listsUrl);
            },
            cache: false,
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

    $.telligent.sharepoint.widgets.browseLists = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);