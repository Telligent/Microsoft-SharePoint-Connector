// $.telligent.sharepoint.widgets.documentLibraryImport
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        groupId: null,
        importUrl: null,
        webUrlHolderId: null,
        webUrlErrorMessage: '',
        libraryHolderId: null,
        libraryErrorMessage: '',
        viewHolderId: null,
        saveButtonId: null,
        errorMessageId: null
    },
    attachHandlers = function (context) {
        // Save
        context.$save = $(context.saveButtonId).click(function (e) {
            e.preventDefault();
            if (context.__locked || !context.$save.evolutionValidation('isValid')) return;

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
        context.validateLibrary = context.$save.evolutionValidation('addCustomValidation', 'library', function () {
            context.libraryId = $(context.libraryHolderId).val();
            return context.libraryId && context.libraryId.length > 0;
        }, context.libraryErrorMessage, $(context.libraryHolderId).closest('.field-item').find('.field-item-validation'), null);

        // Enable a confirmation message
        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register(context.$save);

        // WebURL Control
        $.telligent.sharepoint.controls.glowWebUrl({
            textbox: context.webUrlHolderId
        }).bind('glowLookUpTextBoxChange', function () {
            $(context.libraryHolderId).glowLookUpTextBox('removeByIndex', 0).glowLookUpTextBox('disabled', $(context.webUrlHolderId).val() === '');
            context.validateWebUrl();
        });

        // LibraryId Control
        $.telligent.sharepoint.controls.glowLibrary({
            textbox: context.libraryHolderId,
            get_webUrl: function () {
                return $(context.webUrlHolderId).val();
            }
        }).bind('glowLookUpTextBoxChange', function () {
            context.validateLibrary();
        }).glowLookUpTextBox('disabled', $(context.webUrlHolderId).val() === '');

        // ListView Control
        $.telligent.sharepoint.controls.glowListView({
            textbox: context.viewHolderId,
            get_webUrl: function () {
                return $(context.webUrlHolderId).val();
            },
            get_listId: function () {
                return $(context.libraryHolderId).val();
            }
        }).glowLookUpTextBox('disabled', $(context.listHolderId).val() === '');
    },
    save = function (context) {
        context.__locked = true;
        $.telligent.evolution.put({
            url: context.importUrl,
            data: {
                groupId: context.groupId,
                libraryId: context.libraryId,
                webUrl: context.webUrl,
                viewId: $(context.viewHolderId).val()
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
                context.__locked = false;
                context.$save.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    };

    $.telligent.sharepoint.widgets.documentLibraryImport = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.documentLibraryEdit
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        webUrl: null,
        libraryId: null,
        editUrl: null,
        deleteUrl: null,
        webUrlHolderId: null,
        libraryNameId: null,
        libraryDescriptionId: null,
        viewHolderId: null,
        saveButtonId: null,
        deleteModalUrl: null,
        deleteButtonId: null,
        errorMessageId: null,
        afterDeleteRedirectUrl: null
    },
    attachHandlers = function (context) {
        // Save
        context.$save = $(context.saveButtonId).click(function (e) {
            e.preventDefault();
            if (context.__locked || !context.$save.evolutionValidation('isValid')) return;

            save(context);
        });
        // Delete
        context.$delete = $(context.deleteButtonId).click(function (e) {
            e.preventDefault();
            if (context.__locked) return;

            $.glowModal(context.deleteModalUrl, {
                onClose: function (obj) {
                    if (obj && typeof obj.isDelete !== "undefined") {
                        remove(context, obj.isDelete);
                    }
                }
            });
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
        context.validateLibraryName = context.$save.evolutionValidation('addCustomValidation', 'libraryName', function () {
            context.libraryName = $(context.libraryNameId).val();
            return context.libraryName && context.libraryName.length > 0;
        }, context.libraryNameErrorMessage, $(context.libraryNameId).closest('.field-item').find('.field-item-validation'), null);

        // Enable a confirmation message
        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register(context.$save);
        $.telligent.evolution.navigationConfirmation.register(context.$delete);

        $(context.libraryNameId).change(function () {
            context.validateLibraryName();
        });

        // Default List View Control
        var viewName = $(context.viewHolderId).data('name');
        $.telligent.sharepoint.controls.glowListView({
            textbox: context.viewHolderId,
            webUrl: context.webUrl,
            listId: context.libraryId,
            value: viewName && viewName.length > 0 ? viewName : null
        }).glowLookUpTextBox('disabled', $(context.listHolderId).val() === '');
    },
    save = function (context) {
        context.__locked = true;
        $.telligent.evolution.post({
            url: context.editUrl,
            data: {
                libraryId: context.libraryId,
                libraryName: context.libraryName,
                libraryDescription: context.get_libraryDescription(),
                viewId: $(context.viewHolderId).val()
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
                context.__locked = false;
                context.$save.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    },
    remove = function (context, isDelete) {
        context.__locked = true;
        context.$delete.addClass('disabled').parent().find('.processing').show();
        $.telligent.evolution.del({
            url: context.deleteUrl,
            data: {
                libraryId: context.libraryId,
                isDelete: isDelete
            },
            success: function (response) {
                if (response && response.valid) {
                    window.location = context.afterDeleteRedirectUrl;
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
                context.__locked = false;
                context.$delete.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    };

    $.telligent.sharepoint.widgets.documentLibraryEdit = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.documentLibraryDelete
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    $.telligent.sharepoint.widgets.documentLibraryDelete = {
        register: function (context) {
            $(context.deleteButtonId).click(function (e) {
                e.preventDefault();
                global.parent.$.glowModal.opener(global).$.glowModal.close({
                    isDelete: $(context.isDeleteHolderId).is(':checked')
                });
            });
        }
    };
})(jQuery, window);
