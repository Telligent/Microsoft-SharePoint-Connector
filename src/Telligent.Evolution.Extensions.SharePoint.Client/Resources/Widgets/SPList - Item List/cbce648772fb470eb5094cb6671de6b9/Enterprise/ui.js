(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        visible: false,
        sortBy: '',
        sortOrder: '',
        page: 0,
        viewId: '',
        listItemsUrl: null,
        deleteItemUrl: null,
        confirmDeleteMsg: '',
        deleteFailedMsg: '',
        noSelectedItemsMsg: '',
        pagerFormatter: ''
    },
    listsAdministrationEvent = {
        added: 'sharepointListAdded',
        removed: 'sharepointListRemoved'
    },
    init = function (context) {
        String.prototype.updateQueryString = function (key, value) {
            var uri = this;
            var re = new RegExp("([?|&])" + key + "=.*?(&|$)", "i");
            var separator = uri.indexOf('?') !== -1 ? "&" : "?";
            if (uri.match(re)) {
                return uri.replace(re, '$1' + key + "=" + value + '$2');
            }
            else {
                return uri + separator + key + "=" + value;
            }
        };
        var listsChangedEventHandler = function (context, listsCount, list) {
            if (listsCount == 1) {
                context.wrapper.show();
                context.listItemsUrl = context.listItemsUrl.updateQueryString('listId', list.id).updateQueryString('webUrl', encodeURIComponent(list.spwebUrl));
                context.deleteItemUrl = context.deleteItemUrl.updateQueryString('listId', list.id).updateQueryString('webUrl', encodeURIComponent(list.spwebUrl));
                refresh(context);
            }
            else {
                context.wrapper.hide();
            }
        };

        if (context.visible) {
            context.wrapper.show();
        } else {
            context.wrapper.hide();
        }

        $(document).bind(listsAdministrationEvent.added, function (e, listAddedEventArgs) {
            listsChangedEventHandler(context, listAddedEventArgs.count, listAddedEventArgs.list);
        }).bind(listsAdministrationEvent.removed, function (e, listRemovedEventArgs) {
            if (listRemovedEventArgs.count == 0) {
                context.wrapper.hide();
            }
        });
    },
    refresh = function (context) {
        $.telligent.evolution.get({
            url: context.listItemsUrl,
            data: {
                viewId: context.viewId,
                page: context.page,
                sortBy: context.sortBy,
                sortOrder: context.sortOrder
            },
            success: function (htmlResponse) {
                $('.sharepoint-lists', context.wrapper).html(htmlResponse);
                attachHandlers(context);
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
    },
    attachHandlers = function (context) {
        // Select checkbox on a list item click
        $(".table-list .table-item", context.wrapper).click(function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $tr = $(this).closest("tr");
            var $cb = $tr.find("td.item-checker :checkbox");
            if ($cb.is(':checked')) {
                $cb.attr("checked", false).css("visibility", "");
                $tr.addClass('selected');
            }
            else {
                $cb.attr("checked", true).css("visibility", "visible");
                $tr.removeClass('selected');
            }
        });

        // Update view selector
        $("select.nav-list-view", context.wrapper).each(function () {
            if ($(this).val() == context.viewId) {
                $(this).select();
            }
        });

        // Refresh List, when a List View has been changed
        $("select.nav-list-view", context.wrapper).evolutionSort().bind('evolutionSort', function (e, sort) {
            context.viewId = sort.prop;
            refresh(context);
        });

        // Update an item checker visibility
        $("td.item-checker :checkbox", context.wrapper).click(function (e) {
            e.stopPropagation();
            $(this).css("visibility", this.checked ? "visible" : "");
        });

        // Select all items
        $(".table-header .item-checker :checkbox", context.wrapper).click(function (e) {
            e.stopPropagation();
            if (this.checked) {
                $(this).closest("table").find("td.item-checker :checkbox").attr("checked", true).css("visibility", "visible");
            }
            else {
                $(this).closest("table").find("td.item-checker :checkbox").attr("checked", false).css("visibility", "");
            }
        });

        // Sort Items on a header click
        $(".table-header th:not(.item-checker, .moderation-cell)", context.wrapper).click(function (e) {
            e.preventDefault();
            context.sortBy = $(this).attr("sortBy");
            context.sortOrder = $(this).attr("sortOrder") == "ascending" ? "descending" : "ascending";
            refresh(context);
        });

        // Delete selected items
        $(".footer .btn-delete", context.wrapper).click(function (e) {
            e.preventDefault();
            var ids = [];
            $(context.wrapper).find("td.item-checker :checkbox:checked").each(function (i, o) {
                ids[i] = o.value;
            });

            if (ids.length == 0) {
                window.parent.$.telligent.evolution.notifications.show(context.noSelectedItemsMsg, { type: 'warning' });
                return;
            }

            if (confirm(context.confirmDeleteMsg)) {
                $.telligent.evolution.del({
                    url: context.deleteItemUrl,
                    data: {
                        itemIds: ids.join(",")
                    },
                    success: function (deleteOperation) {
                        if (deleteOperation && deleteOperation.valid) {
                            refresh(context);
                        } else {
                            window.parent.$.telligent.evolution.notifications.show(context.deleteFailedMsg, { type: 'error' });
                        }
                    },
                    error: function (xhr, textStatus, errorThrown) {
                        console.log(textStatus);
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
        });

        // Update pager UI
        var $pager = $(".footer .pager", context.wrapper);
        var totalItems = parseInt($pager.attr("data-totalitems"));
        var pageSize = parseInt($pager.attr("data-pagesize"));
        $pager.find("a.page").attr("href", "#").click(function (e) {
            e.preventDefault();
            context.page = parseInt($(this).attr("data-page")) - 1;
            refresh(context);
        }).each(function () {
            var $a = $(this);
            var idx = parseInt($a.attr("data-page")) - 1;
            var first = pageSize * idx + 1;
            var last = pageSize * idx + pageSize;

            var pagerTitle = context.pagerFormatter
                .replace(/\{0\}/g, first)
                .replace(/\{1\}/g, last > totalItems ? totalItems : last)
                .replace(/\{2\}/g, totalItems);

            $a.attr("title", pagerTitle);
        });

        $(context.wrapper).on('mouseenter', ".show-attachment-list", function () {
            $(this).data('over', true);
        }).on('mouseleave', ".show-attachment-list", function () {
            $(this).data('over', false);
        }).on('glowDelayedMouseEnter', ".show-attachment-list", 100, function () {
            showPopup(context, this);
        }).on('glowDelayedMouseLeave', ".show-attachment-list", 500, function (e) {
            e.stopPropagation();
            if (!context.currentElement || context.currentElement === e.target) {
                closePopup(context);
            }
        });

        $(document).on('mouseenter', ".attachment-list-popup", function () {
            context.over = true;
        }).on('glowDelayedMouseLeave', ".attachment-list-popup", 500, function (e) {
            e.stopPropagation();
            context.over = false;
            closePopup(context);
        });
    },
    showPopup = function (context, element) {
        var hoveredElement = element;
        if (context.currentElement === element) {
            return;
        }

        // if no longer over, don't even bother
        if (!$(hoveredElement).data('over')) {
            return;
        }

        // if a different hover is already open, then first close it
        if (context.currentElement && context.currentElement !== element) {
            close(context);
        }

        // if this is a request to re-open the current open panel, ignore it
        if (context.currentElement && context.currentElement === element && context.popup.glowPopUpPanel('isShown')) {
            return;
        }

        context.currentElement = element;
        context.popup.glowPopUpPanel('html', '');

        var attachmentsHtml = $(element).closest('td').find('.attacment-list-holder').html();
        context.popup.glowPopUpPanel('html', attachmentsHtml).glowPopUpPanel('show', element, false, true);
        return;
    },
    closePopup = function (context) {
        if (!context.over) {
            context.popup.glowPopUpPanel('hide');
            context.currentElement = null;
        }
    };

    $.telligent.sharepoint.widgets.lists = {
        register: function (context) {
            var options = $.extend({}, defaultOptions, context);
            init(options);
            attachHandlers(options);
            options.popup = $('<div></div>').glowPopUpPanel({
                cssClass: 'attachment-list-popup',
                position: 'downcenter',
                zIndex: 1000,
                hideOnDocumentClick: true
            }).glowPopUpPanel('html', '');
        }
    };
})(jQuery);
