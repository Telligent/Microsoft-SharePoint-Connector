// $.telligent.sharepoint.widgets.listImport
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        groupId: null,
        importUrl: null,
        webUrlHolderId: null,
        webUrlErrorMessage: '',
        listHolderId: null,
        listErrorMessage: '',
        viewHolderId: '',
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
        context.validateList = context.$save.evolutionValidation('addCustomValidation', 'list', function () {
            context.listId = $(context.listHolderId).val();
            return context.listId && context.listId.length > 0;
        }, context.listErrorMessage, $(context.listHolderId).closest('.field-item').find('.field-item-validation'), null);

        // Enable a confirmation message
        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register(context.$save);

        // WebURL Control
        $.telligent.sharepoint.controls.glowWebUrl({
            textbox: context.webUrlHolderId
        }).bind('glowLookUpTextBoxChange', function () {
            var isValid = context.validateWebUrl();
            $(context.listHolderId).glowLookUpTextBox('removeByIndex', 0).glowLookUpTextBox('disabled', !isValid);
            $(context.viewHolderId).glowLookUpTextBox('removeByIndex', 0).glowLookUpTextBox('disabled', true);
        });

        // ListId Control
        $.telligent.sharepoint.controls.glowList({
            textbox: context.listHolderId,
            get_webUrl: function () {
                return $(context.webUrlHolderId).val();
            }
        }).bind('glowLookUpTextBoxChange', function () {
            var isValid = context.validateList();
            $(context.viewHolderId).glowLookUpTextBox('removeByIndex', 0).glowLookUpTextBox('disabled', !isValid);
        }).glowLookUpTextBox('disabled', $(context.webUrlHolderId).val() === '');

        // ListView Control
        $.telligent.sharepoint.controls.glowListView({
            textbox: context.viewHolderId,
            get_webUrl: function () {
                return $(context.webUrlHolderId).val();
            },
            get_listId: function () {
                return $(context.listHolderId).val();
            }
        }).glowLookUpTextBox('disabled', $(context.listHolderId).val() === '');
    },
    save = function (context) {
        context.__locked = true;
        $.telligent.evolution.put({
            url: context.importUrl,
            data: {
                groupId: context.groupId,
                listId: context.listId,
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

    $.telligent.sharepoint.widgets.listImport = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.listEdit
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        webUrl: null,
        listId: null,
        editUrl: null,
        deleteUrl: null,
        listNameId: null,
        listDescriptionId: null,
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
        context.validateListName = context.$save.evolutionValidation('addCustomValidation', 'listName', function () {
            context.listName = $(context.listNameId).val();
            return context.listName && context.listName.length > 0;
        }, context.listNameErrorMessage, $(context.listNameId).closest('.field-item').find('.field-item-validation'), null);

        // Enable a confirmation message
        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register(context.$save);
        $.telligent.evolution.navigationConfirmation.register(context.$delete);

        $(context.listNameId).change(function () {
            context.validateListName();
        });

        // Default List View Control
        var viewName = $(context.viewHolderId).data('name');
        $.telligent.sharepoint.controls.glowListView({
            textbox: context.viewHolderId,
            webUrl: context.webUrl,
            listId: context.listId,
            value: viewName && viewName.length > 0 ? viewName : null
        }).glowLookUpTextBox('disabled', $(context.listHolderId).val() === '');
    },
    save = function (context) {
        context.__locked = true;
        $.telligent.evolution.post({
            url: context.editUrl,
            data: {
                listId: context.listId,
                listName: context.listName,
                listDescription: context.get_listDescription(),
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
                listId: context.listId,
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

    $.telligent.sharepoint.widgets.listEdit = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.listDelete
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    $.telligent.sharepoint.widgets.listDelete = {
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
