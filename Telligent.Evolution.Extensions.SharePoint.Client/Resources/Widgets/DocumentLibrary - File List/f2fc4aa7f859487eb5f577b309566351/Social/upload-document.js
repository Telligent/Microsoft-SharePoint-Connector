(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    var defaultOptions = {
        wrapper: document,
        uploader: '.file-uploader',
        uploadUrl: null,
        createDocumentUrl: null,
        overwriteCheckBox: '.overwrite input',
        fileNameInput: '.fileName input',
        saveButton: '.save-button',
        invalidCharactersInFileNameMsg: 'The file name contains invalid characters.'
    },
    attachHandlers = function (context) {
        $(context.uploader, context.wrapper).glowUpload({
            uploadUrl: context.uploadUrl
        }).bind("glowUploadComplete", function (e, f) {
            if (f && f.name.length > 0) {
                $(context.fileNameInput, context.wrapper).val(f.name);
            }
            validate(context);
        });

        $(context.fileNameInput, context.wrapper).change(function (e) {
            validate(context);
        }).keyup(function (e) {
            validate(context);
        });

        $(context.saveButton, context.wrapper).click(function (e) {
            e.preventDefault();
            save(context);
        });

        $(global.document).keyup(function (e) {
            switch (e.keyCode) {
                case 13: save(context); break;
                case 27: window.parent.$.glowModal.opener(window).$.glowModal.close(); break;
            }
        });
    },
    save = function (context) {
        if ($(context.saveButton, context.wrapper).hasClass('disabled') || !validate(context)) return;

        $(context.saveButton, context.wrapper).addClass('disabled');
        $.telligent.evolution.post({
            url: context.createDocumentUrl,
            data: {
                fileName: $(context.uploader, context.wrapper).glowUpload('val').name,
                destinationFileName: $(context.fileNameInput, context.wrapper).val(),
                overwrite: $(context.overwriteCheckBox, context.wrapper).is(':checked')
            },
            success: function (response) {
                if (response && response.valid) {
                    window.parent.$.glowModal.opener(window).$.glowModal.close(response);
                }
                else if (response && response.warningMsg) {
                    $(context.fileNameItem).show();
                    $(".field-item-validation", context.fileNameItem).text(response.warningMsg).show();
                }
            },
            complete: function () {
                $(context.saveButton, context.wrapper).removeClass('disabled');
            },
            error: function (xhr, textStatus) {
                var errorMsg = "";
                try {
                    var responseText = $.parseJSON(xhr.responseText);
                    if (responseText != null) { errorMsg = responseText.Errors.join('<br/>'); }
                }
                catch (ex) {
                    errorMsg = textStatus;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    validateUplodedFile = function (context) {
        var uploadedFile = $(context.uploader, context.wrapper).glowUpload('val');
        var uploadedFileIsValid = uploadedFile && uploadedFile.name && uploadedFile.name.length > 0;
        if (uploadedFileIsValid) {
            $(context.uploader, context.wrapper).closest('.field-item').find('.field-item-validation').hide();
        } else {
            $(context.uploader, context.wrapper).closest('.field-item').find('.field-item-validation').show();
        }
        return uploadedFileIsValid;
    },
    validateFileName = function (context) {
        var forbiddenFileNamePattern = /(^\.|\.$)|(\.\.+)|[~"#%&*:<>\?\/\\\{|\}]/ig;
        var fileName = $(context.fileNameInput, context.wrapper).val();
        var fileNameIsValid = fileName && fileName.length > 0 && !forbiddenFileNamePattern.test(fileName);
        if (fileNameIsValid) {
            $(context.fileNameInput, context.wrapper).focus().closest('.field-item').find('.field-item-validation').hide();
        }
        else {
            $(context.fileNameInput, context.wrapper).focus().closest('.field-item').find('.field-item-validation').text(context.invalidCharactersInFileNameMsg).show();
        }
        return fileNameIsValid;
    },
    validate = function (context) {
        var fileUploadedIsValid = validateUplodedFile(context);
        var fileNameIsValid = validateFileName(context);

        if (fileUploadedIsValid && fileNameIsValid) {
            $(context.saveButton, context.wrapper).removeClass('disabled');
        }
        else {
            $(context.saveButton, context.wrapper).addClass('disabled');
        }

        return fileUploadedIsValid && fileNameIsValid;
    };

    $.telligent.sharepoint.widgets.documentLibrary.upload = {
        register: function (context) {
            attachHandlers($.extend({}, defaultOptions, context));
        }
    };
})(jQuery, window);