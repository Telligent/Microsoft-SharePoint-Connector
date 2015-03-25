(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        wrapper: document,
        folderNameTextBox: null,
        createEditFolderUrl: null,
        saveButton: '.save-button'
    },
    attachHandlers = function (context) {

        $(context.folderNameTextBox, context.wrapper).change(function () {
            validate(context);
        }).keyup(function (e) {
            var isValid = validate(context);
            if (e.keyCode == '13' && isValid) {
                save(context);
            } else if (e.keyCode == '27') {
                window.parent.$.glowModal.opener(window).$.glowModal.close();
            }
        });

        $(context.saveButton, context.wrapper).click(function (e) {
            e.preventDefault();
            if (validate(context)) {
                save(context);
            };
        });

    },
    save = function (context) {
        $(context.saveButton, context.wrapper).addClass('disabled');
        $.telligent.evolution.post({
            url: context.createEditFolderUrl,
            data: {
                folderName: $(context.folderNameTextBox, context.wrapper).val()
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
                $(context.saveButton, context.wrapper).removeClass('disabled');
            },
            error: function (xhr, textStatus) {
                var errorMsg;
                try {
                    errorMsg = eval('(' + xhr.responseText + ')').Errors.join('<br/>');
                }
                catch (ex) {
                    errorMsg = textStatus;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    validate = function (context) {
        var value = $(context.folderNameTextBox, context.wrapper).val();
        var validator = /^\.|\.$|(\.\.)|~|"|#|%|&|\*|:|<|>|\?|\/|\\|\{|\}|\|/;
        var isValid = value && value.length > 0 && !validator.test(value);
        if (isValid) {
            $(context.saveButton, context.wrapper).removeClass('disabled');
            $(context.folderNameTextBox, context.wrapper).closest('.field-item').find('.field-item-validation').hide();
        }
        else {
            $(context.saveButton, context.wrapper).addClass('disabled');
            $(context.folderNameTextBox, context.wrapper).closest('.field-item').find('.field-item-validation').show();
        }
        return isValid;
    };

    $.telligent.sharepoint.widgets.documentLibrary.folder = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            $(context.folderNameTextBox, context.wrapper).focus();
            if (context.onloadValidationEnabled) {
                validate(context);
            }
            attachHandlers(context);
        }
    };
})(jQuery, window);