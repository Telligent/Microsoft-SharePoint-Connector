(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        deleteVerificationText: "",
        deleteListItemUrl: null
    },
    deleteListItem = function (context, contentId) {
        if (confirm(context.deleteVerificationText)) {
            $.telligent.evolution.del({
                url: context.deleteListItemUrl,
                data: {
                    contentId: contentId
                },
                dataType: 'json',
                success: function (response) {
                    if (response && response.valid) {
                        window.parent.$.telligent.evolution.notifications.show(response.statusMsg, { type: 'warning' });
                        setTimeout(function () {
                            window.location = response.UrlRedirect;
                        }, 1000);
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    console.log(textStatus);
                    window.parent.$.telligent.evolution.notifications.show(textStatus, { type: 'error' });
                }
            });
        }
    };

    $.telligent.sharepoint.widgets.listItemProperties = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            $.telligent.evolution.messaging.subscribe('delete-listItem', function (data) {
                deleteListItem(context, $(data.target).data('contentid'));
            })
        }
    };
})(jQuery);