(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        checkInModalUrl: null,
        checkInUrl: null,
        checkedOutId: null,
        checkedOutUrl: null,
        deleteVerificationText: "You are about to delete this document, would you like to continue?",
        deleteDocumentUrl: null
    },
    init = function (context) {
        context.$checkedOut = $(context.checkedOutId);
    },
    attachHandlers = function (context) {

        var getUILinks = function (target) {
            var $uiLinks = $(target).closest('ul');
            return {
                chickIn: $uiLinks.find('a[data-type="checkIn"]'),
                checkOut: $uiLinks.find('a[data-type="checkOut"]'),
                discardCheckOut: $uiLinks.find('a[data-type="discardCheckOut"]')
            };
        };

        $.telligent.evolution.messaging.subscribe('checkInSubscribe', function (e) {
            $.glowModal(context.checkInModalUrl, {
                onClose: function (res) {
                    if (res && !res.isCheckedOut) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.hide();
                        $uiLinks.discardCheckOut.hide();
                        $uiLinks.checkOut.show();
                        context.$checkedOut.slideUp();
                    }
                }
            });
        });

        $.telligent.evolution.messaging.subscribe('discardCheckOutSubscribe', function (e) {
            $.telligent.evolution.post({
                url: context.checkInUrl,
                data: {method: "discardcheckout"},
                success: function (res) {
                    if (res) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.hide();
                        $uiLinks.discardCheckOut.hide();
                        $uiLinks.checkOut.show();
                        context.$checkedOut.slideUp();
                    }
                },
                error: errorHandler
            });
        });

        $.telligent.evolution.messaging.subscribe('checkOutSubscribe', function (e) {
            $.telligent.evolution.post({
                url: context.checkInUrl,
                data: {method: "checkout"},
                success: function (res) {
                    if (res) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.show();
                        $uiLinks.discardCheckOut.show();
                        $uiLinks.checkOut.hide();
                        showCheckedOut(context);
                    }
                },
                error: errorHandler
            });
        });

        $.telligent.evolution.messaging.subscribe('delete-document', function (data) {
            if (global.confirm(context.deleteVerificationText)) {
                $.telligent.evolution.del({
                    url: context.deleteDocumentUrl,
                    data: {
                        documentId: $(data.target).data('contentid')
                    },
                    success: function (response) {
                        if (response && response.valid) {
                            window.parent.$.telligent.evolution.notifications.show(response.statusMsg, { type: 'info' });
                            if (response.UrlRedirect) {
                                global.setTimeout(function () {
                                    window.location = response.UrlRedirect;
                                }, 2500);
                            }
                        }
                    },
                    error: errorHandler
                });
            }
        });
    },
    showCheckedOut = function (context) {
        $.telligent.evolution.get({
            url: context.checkedOutUrl,
            success: function (res) {
                context.$checkedOut.find('.field-item-content').html(res);
                context.$checkedOut.slideDown();
            },
            error: errorHandler
        });
    },
    errorHandler = function (xhr, desc, ex) {
        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
        }
        else {
            $.telligent.evolution.notifications.show(desc, { type: 'error' });
        }
    };
    $.telligent.sharepoint.widgets.document = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.documentCheckIn
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: global.document,
        contentId: null,
        commentId: null,
        get_checkInType: function () {
            var $version = $(".checkin-version .version-list :radio:checked", this.wrapper);
            return $version.length > 0 ? $version.data('type') : null;
        },
        saveId: null,
        saveUrl: null
    },
    attachHandlers = function (context) {
        context.data = {
            contentId: context.contentId,
            method: "checkin"
        };

        var $saveButton = $(context.saveId, context.wrapper).bind("click", function (e) {
            e.preventDefault();

            $saveButton.addClass("disabled").parent().find('.processing').show();
            context.data.keepcout = $(context.keepCheckedOutId, context.wrapper).is(":checked");
            context.data.comment = $(context.commentId, context.wrapper).val();
            context.data.checkintype = context.get_checkInType();

            $.telligent.evolution.put({
                url: context.saveUrl,
                data: context.data,
                success: function () {
                    window.parent.$.glowModal.opener(window).$.glowModal.close({
                        isCheckedOut: context.data.keepcout
                    });
                },
                error: function (xhr, desc, ex) {
                    if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                        $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                    }
                    else {
                        $.telligent.evolution.notifications.show(desc, { type: 'error' });
                    }
                },
                complete: function () {
                    $saveButton.removeClass("disabled").parent().find('.processing').hide();
                }
            });
        });
    };

    $.telligent.sharepoint.widgets.documentCheckIn = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);