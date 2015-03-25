(function ($) {
	$.telligent = $.telligent || {};
	$.telligent.sharepoint = $.telligent.sharepoint || {};
	$.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var hideAllExceptOne = function (context, holdeName) {
        for (var i in context.holders) {
            if (holdeName == context.holders[i]) {
                $(context.holders[i]).show();
            } else {
                $(context.holders[i]).hide();
            }
        }
    },
    attachHandlers = function (context) {
        $(context.isRecurringCheckerId).change(function (e) {
            if (this.checked) {
                //$(context.holderId).find(".form-recurrence").show();
                $(context.holderId).find(".form-recurrence").slideDown("fast");
                $(this).val('Yes');
            }
            else {
                //$(context.holderId).find(".form-recurrence").hide();
                $(context.holderId).find(".form-recurrence").slideUp("fast");
                $(this).val('No');
            }
        });

        $(context.dailySelectorId).change(function (e) {
            if (this.checked) {
                hideAllExceptOne(context, context.dailyHolderId);
            }
        });

        $(context.weeklySelectorId).change(function (e) {
            if (this.checked) {
                hideAllExceptOne(context, context.weeklyHolderId);
            }
        });

        $(context.monthlySelectorId).change(function (e) {
            if (this.checked) {
                hideAllExceptOne(context, context.monthlyHolderId);
            }
        });

        $(context.yearlySelectorId).change(function (e) {
            if (this.checked) {
                hideAllExceptOne(context, context.yearlyHolderId);
            }
        });
    }
    $.telligent.sharepoint.widgets.recurrenceEditor = {
        register: function (context) {
            context.holders = [context.dailyHolderId, context.weeklyHolderId, context.monthlyHolderId, context.yearlyHolderId];
            attachHandlers(context);
        }
    };
})(jQuery);
