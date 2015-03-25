(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        wrapper: document,
        folderNameId: null,
        saveUrl: null,
        saveButtonId: null,
        onloadValidationEnabled: false
    },
    attachHandlers = function (context) {
        $(context.folderNameId, context.wrapper).on("change keyup", function (e) {
            if (validate(context)) {
                if (e.keyCode == '13') {
                    // Enter
                    save(context);
                } else if (e.keyCode == '27') {
                    // Esc
                    window.parent.$.glowModal.opener(window).$.glowModal.close();
                }
            }
        });
        $(context.saveButtonId, context.wrapper).click(function (e) {
            e.preventDefault();
            if (validate(context)) {
                save(context);
            };
        });
    },
    save = function (context) {
        $(context.saveButtonId, context.wrapper).addClass('disabled').parent().find('.processing').show();
        $.telligent.evolution.post({
            url: context.saveUrl,
            data: {
                folderName: $(context.folderNameId, context.wrapper).val()
            },
            success: function (response) {
                if (response && response.valid) {
                    window.parent.$.glowModal.opener(window).$.glowModal.close(response);
                }
                else {
                    window.parent.$.telligent.evolution.notifications.show(response.errorMsg, { type: 'error' });
                }
            },
            complete: function () {
                $(context.saveButtonId, context.wrapper).removeClass('disabled').parent().find('.processing').hide();
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
    },
    validate = function (context) {
        var value = $(context.folderNameId, context.wrapper).val();
        var validator = /^\.|\.$|(\.\.)|~|"|#|%|&|\*|:|<|>|\?|\/|\\|\{|\}|\|/;
        var isValid = value && value.length > 0 && !validator.test(value);
        if (isValid) {
            $(context.saveButtonId, context.wrapper).removeClass('disabled');
            $(context.folderNameId, context.wrapper).closest('.field-item').find('.field-item-validation').hide();
        }
        else {
            $(context.saveButtonId, context.wrapper).addClass('disabled');
            $(context.folderNameId, context.wrapper).closest('.field-item').find('.field-item-validation').show();
        }
        return isValid;
    };

    $.telligent.sharepoint.widgets.documentLibrary.folder = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            $(context.folderNameId, context.wrapper).focus();
            if (context.onloadValidationEnabled) {
                validate(context);
            }
            attachHandlers(context);
        }
    };
})(jQuery, window);