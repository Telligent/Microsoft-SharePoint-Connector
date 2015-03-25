(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        librariesUrl: null,
        defaultSearchText: 'Search',
        searchResultsUrl: null
    },
    librariesAdministrationEvent = {
        added: 'documentLibraryAdded',
        removed: 'documentLibraryRemoved'
    },
    init = function (context) {
        $(global).bind(librariesAdministrationEvent.added, function (e, listAddedEventArgs) {
            // a new document library has been added
            refresh($('.document-library-paged-list', context.wrapper), context.librariesUrl);
        }).bind(librariesAdministrationEvent.removed, function (e, listRemovedEventArgs) {
            // a document library has been removed
            refresh($('.document-library-paged-list', context.wrapper), context.librariesUrl);
        });
        buildEvolutionSort(context.wrapper, context.librariesUrl);
    },
    attachHandlers = function (context) {
        $(context.wrapper).on("click", '.table-header-column', function (e) {
            e.preventDefault();
            if ($('a', this).length > 0) {
                var sortBy = $('a', this).attr('sortBy') || '';
                var sortOrder = $('a', this).attr('sortOrder') || '';
                refresh($('.document-library-paged-list', context.wrapper), context.librariesUrl, {
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
                    refresh($('.document-library-paged-list', context.wrapper), context.librariesUrl);
                }, 500);
            }
        }).on("click", '.internal-link.clear-search', function (e) {
            e.preventDefault();
            $('.field-item-input input', context.wrapper).val('');
            refresh($('.document-library-paged-list', context.wrapper), context.librariesUrl);
        });
    },
    buildEvolutionSort = function (wrapper, librariesUrl) {
        $(".sort-options", wrapper).evolutionSort().bind('evolutionSort', function (e, sort) {
            refresh(wrapper, librariesUrl, {
                w_sortBy: sort.prop,
                w_sortOrder: sort.direction
            });
        });
    },
    refresh = function (wrapper, librariesUrl, data) {
        $.telligent.evolution.get({
            url: librariesUrl,
            data: data,
            success: function (htmlMarkup) {
                $(wrapper).html(htmlMarkup);
                buildEvolutionSort(wrapper, librariesUrl);
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

    $.telligent.sharepoint.widgets.browseLibraries = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);