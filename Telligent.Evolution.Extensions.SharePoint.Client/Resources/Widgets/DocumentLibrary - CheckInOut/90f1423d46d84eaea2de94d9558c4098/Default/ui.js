(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var init = function (context) {
        loadInProgress(context);
        $.telligent.evolution.get({
            url: context.checkInOutUrl,
            dataType: 'json',
            success: function (response) {
                if (response && response.valid) {
                    $(".error", context.wrapperId).hide();
                    context.file = response.file;
                }
                else {
                    $(".error", context.wrapperId).show();
                }
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
                refresh(context);
                loadComplete(context);
            }
        });
    },
    loadInProgress = function (context) {
        $(context.wrapperId).hide();
        $(context.loadingHolderId).show();
    },
    loadComplete = function (context) {
        $(context.wrapperId).show();
        $(context.loadingHolderId).hide();
    },
    attachHandlers = function (context) {
        $(".check-in", context.wrapperId).click(function (e) {
            e.preventDefault();
            $(".check-in-form", context.wrapperId).show();
        });

        $(".discard-check-out", context.wrapperId).click(function (e) {
            e.preventDefault();
            if (confirm(context.discardConfirmMsg)) {
                update(context, { operation: "discardcheckout" });
            }
        });

        $(".check-out", context.wrapperId).click(function (e) {
            e.preventDefault();
            update(context, { operation: "checkout" });
        });

        $(".check-in-form .post-check-in", context.wrapperId).click(function (e) {
            e.preventDefault();
            var checkInData = {
                operation: "checkin",
                keepcout: $(".check-in-form", context.wrapperId).find(".rb-keep:checked").val() == 1,
                checkintype: $(".check-in-form", context.wrapperId).find(".check-in-version :radio:checked").val(),
                comment: $(".check-in-form", context.wrapperId).find(".check-in-comment").val()
            },
            beforeCheckIn = function (ctx) {
                $(".check-in-form", ctx.wrapperId).find(":button").attr("disabled", true);
            },
            afterCheckIn = function (ctx) {
                $(".check-in-form :button", ctx.wrapperId).removeAttr("disabled");
                $(".check-in-comment", ctx.wrapperId).val("");
            };
            update(context, checkInData, beforeCheckIn, afterCheckIn);
        });

        $(".check-in-form .cancel-check-in", context.wrapperId).click(function (e) {
            e.preventDefault();
            $(".check-in-form", context.wrapperId).hide();
        });
    },
    update = function (context, data, beforeUpdateEventHandler, afterUpdateEventHandler) {
        if (beforeUpdateEventHandler && typeof beforeUpdateEventHandler === 'function') {
            beforeUpdateEventHandler(context);
        }
        $.telligent.evolution.put({
            url: context.checkInOutUrl,
            data: data,
            dataType: 'json',
            success: function (response) {
                if (response && response.valid) {
                    $(".error", context.wrapperId).hide();
                    context.file = response.file;
                }
                else {
                    $(".error", context.wrapperId).show();
                }
            },
            error: function (xhr, status) {
                window.parent.$.telligent.evolution.notifications.show(status, { type: 'error' });
            },
            complete: function () {
                refresh(context);
                if (afterUpdateEventHandler && typeof afterUpdateEventHandler === 'function') {
                    afterUpdateEventHandler(context);
                }
            }
        });
    },
    refresh = function (context) {
        $(".check-in-form", context.wrapperId).hide().find(":button").removeAttr("disabled");
        $(".check-in-form .rb-keep[value='0']", context.wrapperId).attr("checked", true);

        if (!context.file) {
            $(".error", context.wrapperId).show();
            return;
        }

        if (context.file.isCheckedOut) {
            $(".check-in", context.wrapperId).show();
            $(".discard-check-out", context.wrapperId).show();
            $(".check-out", context.wrapperId).hide();
            $(".checked-out-status", context.wrapperId).show()
                .find("a.profile").attr("profile-id", context.file.id)
                .attr("href", context.file.checkedOutByUser.url)
                .text(context.file.checkedOutByUser.title);
        }
        else {
            $(".check-in", context.wrapperId).hide().next(".check-in-separator").hide();
            $(".discard-check-out", context.wrapperId).hide();
            $(".check-out", context.wrapperId).show();
            $(".checked-out-status", context.wrapperId).hide();
        }

        var $checkinVer = $(".check-in-form .check-in-version", context.wrapperId);
        $checkinVer.find(":radio").show().removeAttr("checked");
        if (context.file.enableVersioning) {
            $checkinVer.show();
            if (context.file.minorVersion <= 1) {
                $checkinVer.find(".overwrite").hide();
            }
            else {
                $checkinVer.find(".overwrite label").text([context.file.majorVersion, ".", context.file.minorVersion - 1].join(""));
            }
            $checkinVer.find(".major label").text([context.file.majorVersion + 1, ".0"].join(""));
            if (!context.file.enableMinorVersions) {
                $checkinVer.find(".major :radio").attr("checked", true);
            }
        }
        else {
            $checkinVer.find(".overwrite :radio").attr("checked", true);
        }

        if (context.file.enableMinorVersions) {
            $checkinVer.find(".minor label").text([context.file.majorVersion, ".", context.file.minorVersion].join(""));
            $checkinVer.find(".minor :radio").attr("checked", true);
        }
        else {
            $checkinVer.hide();
        }
    };

    $.telligent.sharepoint.widgets.checkInOut = {
        register: function (context) {
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery);