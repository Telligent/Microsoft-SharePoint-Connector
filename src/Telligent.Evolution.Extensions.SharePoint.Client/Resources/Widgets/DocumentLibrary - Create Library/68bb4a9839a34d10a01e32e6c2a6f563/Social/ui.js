// $.telligent.sharepoint.widgets.documentLibraryCreate
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        groupId: null,
        createUrl: null,
        webUrlHolderId: null,
        webUrlErrorMessage: null,
        libraryNameHolderId: null,
        libraryNameErrorMessage: null,
        get_libraryDescription: null,
        isDeleteHolderId: null,
        saveButtonId: null,
        errorMessageId: null
    },
    attachHandlers = function (context) {
        // Save
        context.$save = $(context.saveButtonId).click(function (e) {
            e.preventDefault();
            if (context.__saving || !context.$save.evolutionValidation('isValid')) return;

            save(context);
        });
        // Validation
        context.$save.evolutionValidation({
            validateOnLoad: false,
            onValidated: function (isValid, buttonClicked, c) {
                if (isValid) {
                    context.$save.removeClass('disabled');
                } else {
                    context.$save.addClass('disabled');
                }
            },
            onSuccessfulClick: function (e) {
                context.$save.addClass('disabled').parent().find('.processing').show();
            }
        });
        context.validateWebUrl = context.$save.evolutionValidation('addCustomValidation', 'webUrl', function () {
            context.webUrl = $(context.webUrlHolderId).val();
            return context.webUrl && context.webUrl.length > 0;
        }, context.webUrlErrorMessage, $(context.webUrlHolderId).closest('.field-item').find('.field-item-validation'), null);
        context.validateLibraryName = context.$save.evolutionValidation('addCustomValidation', 'libraryName', function () {
            context.libraryName = $(context.libraryNameHolderId).val();
            return context.libraryName && context.libraryName.length > 0;
        }, context.libraryNameErrorMessage, $(context.libraryNameHolderId).closest('.field-item').find('.field-item-validation'), null);

        // Enable a confirmation message
        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register(context.$save);

        // WebURL Control
        $.telligent.sharepoint.controls.glowWebUrl({
            textbox: context.webUrlHolderId
        }).bind('glowLookUpTextBoxChange', function () {
            $(context.libraryNameHolderId).glowLookUpTextBox('removeByIndex', 0).glowLookUpTextBox('disabled', $(context.webUrlHolderId).val() === '');
            context.validateWebUrl();
        });

        $(context.libraryNameHolderId).change(function () {
            context.validateLibraryName();
        });
    },
    save = function (context) {
        context.__saving = true;
        $.telligent.evolution.put({
            url: context.createUrl,
            data: {
                groupId: context.groupId,
                webUrl: context.webUrl,
                libraryName: context.libraryName,
                libraryDescription: context.get_libraryDescription()
            },
            success: function (response) {
                if (response && response.valid && response.redirectUrl) {
                    window.location = response.redirectUrl;
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
                context.__saving = false;
                context.$save.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    };

    $.telligent.sharepoint.widgets.documentLibraryCreate = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options)
            attachHandlers(context);
        }
    };
})(jQuery, window);
