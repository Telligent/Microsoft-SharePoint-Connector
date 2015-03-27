(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        createListItemUrl: null,
        editListItemUrl: null
    },
    $createButton,
    $updateButton,
    $deleteButton,
    beforeSaveValidations = [],
    beforeSaveEventHandlers = [],
    attachHandlers = function (context) {
        $createButton = $(".internal-link.create-post", context.wrapper).click(function (e) {
            e.preventDefault();
            if (!$(this).hasClass('disabled')) {
                createListItem(context);
            }
        });
        $updateButton = $(".internal-link.update-post", context.wrapper).click(function (e) {
            e.preventDefault();
            if (!$(this).hasClass('disabled')) {
                updateListItem(context);
            }
        });
        $deleteButton = $(".internal-link.delete-post", context.wrapper).click(function (e) {
            e.preventDefault();
            if (!$(this).hasClass('disabled')) {
                deleteListItem(context);
            }
        });
    },
    getFormData = function (form) {
        var data = {};
        form.find('textarea[name], :text[name], select[name], :hidden[name], :radio[name]:checked').each(function () {
            var key = $(this).attr('name');
            var val = $(this).val();
            if (val == "[object Object]") val = "";
            if (val.length == 0 && $.telligent.sharepoint.widgets.personOrGroupEditor) {
                val = $.telligent.sharepoint.widgets.personOrGroupEditor.getValues({
                    personOrGroupTextbox: ["#", $(this).attr('id')].join(''),
                    selectedLookUps: [".", $(this).attr('id'), "SelectedLookUps"].join('')
                });
            }
            data[key] = val;
        });
        var cbData = {};
        form.find(':checkbox[name]:checked').each(function () {
            var key = $(this).attr('name');
            var val = $(this).val();
            if (cbData[key]) {
                cbData[key].push(val);
            }
            else {
                cbData[key] = [val];
            }
        });
        for (var i in cbData) {
            cbData[i] = cbData[i].join(",");
        }
        $.extend(data, cbData);
        return data;
    },
    onBeforeSave = function () {
        for (var i = 0, len = beforeSaveEventHandlers.length; i < len; i++) {
            if (typeof (beforeSaveEventHandlers[i]) === "function") {
                beforeSaveEventHandlers[i]();
            }
        }
    },
    createListItem = function (context) {
        onBeforeSave();
        if (!validate()) return;
        var formData = getFormData(context.wrapper);

        var $createPost = $(".internal-link.create-post", context.wrapper);
        $createPost.addClass('disabled').parent().find('.processing').show();

        $.telligent.evolution.put({
            url: context.createListItemUrl,
            data: formData,
            dataType: 'json',
            success: function (response) {
                if (response && response.valid) {
                    $(global).trigger('sharepoint_list_updated', response);
                    window.location = response.listItem.UrlRedirect;
                } else if (response) {
                    window.parent.$.telligent.evolution.notifications.show(response.statusMsg, { type: 'warning' });
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
                $createPost.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    },
    updateListItem = function (context) {
        onBeforeSave();
        if (!validate()) return;
        var formData = getFormData(context.wrapper);

        $updateButton.addClass('disabled').parent().find('.processing').show();
        $deleteButton.addClass('disabled');

        $.telligent.evolution.post({
            url: context.editListItemUrl,
            data: formData,
            dataType: 'json',
            success: function (response) {
                if (response && response.valid) {
                    $(global).trigger('sharepoint_list_updated', response);
                    window.location = response.listItem.UrlRedirect;
                } else if (response) {
                    window.parent.$.telligent.evolution.notifications.show(response.statusMsg, { type: 'warning' });
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
                $updateButton.removeClass("disabled").parent().find('.processing').hide();
                $deleteButton.removeClass("disabled");
            }
        });
    },
    deleteListItem = function (context) {
        $deleteButton.addClass('disabled').parent().find('.processing').show();
        $.telligent.evolution.del({
            url: context.deleteUrl,
            success: function (response) {
                if (response && response.valid) {
                    window.location = context.listUrl;
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
                $deleteButton.removeClass('disabled').parent().find('.processing').hide();
            }
        });
    },
    validate = function () {
        var isValid = true;
        for (var i = 0, len = beforeSaveValidations.length; i < len; i++) {
            if (typeof (beforeSaveValidations[i]) === "function") {
                isValid = isValid && beforeSaveValidations[i]();
            }
        }
        return isValid;
    };

    $.telligent.sharepoint.widgets.createListItem = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };

    $.telligent.sharepoint.widgets.editListItem = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
            validate(context);
        }
    };

    $.telligent.sharepoint.widgets.listItem = {
        registerValidation: function (validationFunc) {
            beforeSaveValidations.push(validationFunc);
        },
        registerInputValidation: function (editor, errorMsg, validationFunc) {
            var isValid = function (input) {
                return validationFunc(input);
            },
            validateInputValue = function (input, msg) {
                if (isValid(input)) {
                    input.removeClass('invalid');
                    msg.hide();
                    return true;
                }
                else {
                    input.addClass("invalid").focus();
                    msg.show();
                    return false;
                }
            },
            attachValidationHandlers = function (input, msg) {
                input.bind('keyup', function (e) {
                    e.stopPropagation();
                    validateInputValue(input, msg);
                }).bind('change', function (e) {
                    e.stopPropagation();
                    validateInputValue(input, msg);
                });
            };
            attachValidationHandlers(editor, errorMsg);
            beforeSaveValidations.push(function () {
                return validateInputValue(editor, errorMsg);
            });
        },
        registerBeforeSaveEventHandler: function (event) {
            beforeSaveEventHandlers.push(event);
        }
    };
})(jQuery, window);
