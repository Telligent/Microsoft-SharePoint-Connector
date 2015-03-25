//------------------------------------//
// $.telligent.sharepoint.widgets.lists
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        listViewUrl: null,
        viewId: null,
        listViewHolder: ".sharepoint-list-view",
        listUrl: null,
        listHolder: ".sharepoint-list-view .sharepoint-list-holder",
        sortBy: '',
        sortOrder: '',
        page: 0,
        editListUrl: null,
        deleteUrl: null,
        confirmDeleteMsg: '',
        deleteSuccessMsg: '',
        deleteFailedMsg: '',
        noSelectedItemsMsg: '',
        pagerFormatter: ''
    },
    subscribe = function (context) {
        $.telligent.evolution.messaging.subscribe("deleteSelectedItemsSubscribe", function () {
            deleteSelectedItems(context, $.map($(".table-list .table-item", context.wrapper).find(".item-checker :checkbox:checked"), function (checkbox) {
                return $(checkbox).data('id');
            }));
        });

        $.telligent.evolution.messaging.subscribe("editListSubscribe", function () {
            window.location.href = context.editListUrl;
        });
    },
    setupHeaderLinks = function (context) {
        context.viewId = $('.view select', context.wrapper).val();
        $('.view select', context.wrapper).on('change', function (e) {
            context.viewId = $(e.target).val();
            refreshListView(context);
        });

        context.sortBy = $('.sort .by select', context.wrapper).val();
        $('.sort .by select', context.wrapper).on('change', function (e) {
            context.sortBy = $('.sort .by select', context.wrapper).val();
            refreshList(context);
        });

        context.sortOrder = $('.sort .order select', context.wrapper).val();
        $('.sort .order select', context.wrapper).on('change', function (e) {
            context.sortOrder = $('.sort .order select', context.wrapper).val();
            refreshList(context);
        });
    },
    refreshListView = function (context) {
        $.telligent.evolution.get({
            url: context.listViewUrl,
            data: {
                viewId: context.viewId,
                page: context.page,
                sortBy: context.sortBy,
                sortOrder: context.sortOrder
            },
            success: function (htmlResponse) {
                $(context.listViewHolder, context.wrapper).html(htmlResponse);
                setupHeaderLinks(context);
                attachHandlers(context);
            },
            error: handleError
        });
    },
    refreshList = function (context) {
        $.telligent.evolution.get({
            url: context.listUrl,
            data: {
                viewId: context.viewId,
                page: context.page,
                sortBy: context.sortBy,
                sortOrder: context.sortOrder
            },
            success: function (htmlResponse) {
                $(context.listHolder, context.wrapper).html(htmlResponse);
                attachHandlers(context);
            },
            error: handleError
        });
    },
    deleteSelectedItems = function (context, itemIds) {
        if (itemIds.length === 0) {
            window.parent.$.telligent.evolution.notifications.show(context.noSelectedItemsMsg, { type: 'info' });
            return;
        }
        if (window.confirm(context.confirmDeleteMsg)) {
            $.telligent.evolution.del({
                url: context.deleteUrl,
                data: {
                    itemIds: itemIds.join(",")
                },
                success: function (deleteOperation) {
                    if (deleteOperation && deleteOperation.valid) {
                        refreshList(context);
                        window.parent.$.telligent.evolution.notifications.show(context.deleteSuccessMsg, { type: 'info' });
                        return;
                    }
                    window.parent.$.telligent.evolution.notifications.show(context.deleteFailedMsg, { type: 'error' });
                },
                error: handleError
            });
        }
    },
    handleError = function (xhr, desc, ex) {
        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
        }
        else {
            $.telligent.evolution.notifications.show(desc, { type: 'error' });
        }
    },
    attachHandlers = function (context) {
        // Select checkbox on a list item click
        $(".table-list .table-item", context.wrapper).click(function (e) {
            var $tr = $(this).closest("tr"),
            $cb = $tr.find(".item-checker input:checkbox");
            if (e.target.nodeName !== "INPUT" && e.target.nodeName !== "A") {
                $cb[0].checked = !$cb[0].checked;
            }
            if ($cb.is(':checked')) {
                $tr.addClass('selected');
            }
            else {
                $tr.removeClass('selected');
            }
        });

        // Select all items
        $(".table-header .item-checker :checkbox", context.wrapper).click(function (e) {
            e.stopPropagation();
            var $table = $(this).closest("table");
            if (this.checked) {
                $(".table-item", $table).addClass('selected').find(".item-checker :checkbox").attr("checked", "checked")
            }
            else {
                $(".table-item", $table).removeClass('selected').find(".item-checker :checkbox").removeAttr("checked")
            }
        });

        $(".table-list .table-item .moderation-menu .delete", context.wrapper).click(function (e) {
            e.preventDefault();
            e.stopPropagation();
            deleteSelectedItems(context, [$(this).data('id')]);
        });

        // Update pager UI
        var $pager = $(".pager", context.wrapper),
        totalItems = +$pager.data("totalitems"),
        pageSize = +$pager.data("pagesize");
        $pager.find("a[data-page]").attr("href", "#").click(function (e) {
            e.preventDefault();
            context.page = +$(this).data("page") - 1;
            refreshList(context);
        }).each(function () {
            var $a = $(this),
                idx = +$a.data("page") - 1,
                first = pageSize * idx + 1,
                last = pageSize * idx + pageSize,
                pagerTitle = context.pagerFormatter
                    .replace(/\{0\}/g, first)
                    .replace(/\{1\}/g, last > totalItems ? totalItems : last)
                    .replace(/\{2\}/g, totalItems);
            $a.attr("title", pagerTitle);
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
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            subscribe(context);
            setupHeaderLinks(context);
            attachHandlers(context);
            context.popup = $('<div></div>').glowPopUpPanel({
                cssClass: 'attachment-list-popup',
                position: 'downcenter',
                zIndex: 1000,
                hideOnDocumentClick: true
            }).glowPopUpPanel('html', '');
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
        }
    };
})(jQuery, window);
