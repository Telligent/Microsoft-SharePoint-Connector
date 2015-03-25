// $.telligent.sharepoint.widgets.validator
(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var beforeSaveValidations = [],
    beforeSaveEventHandlers = [],
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
            if (cbData[key])
                cbData[key].push(val);
            else
                cbData[key] = [val];
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
    validate = function () {
        var isValid = true;
        for (var i = 0, len = beforeSaveValidations.length; i < len; i++) {
            if (typeof (beforeSaveValidations[i]) === "function") {
                isValid = isValid && beforeSaveValidations[i]();
            }
        }
        return isValid;
    };

    $.telligent.sharepoint.widgets.validator = {
        validateAndGetFormData: function (w) {
            var result = {
                isValid: false,
                data: null
            };

            onBeforeSave();

            if (validate()) {
                result.isValid = true;
                result.data = getFormData(w);
            }

            return result;
        },
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
})(jQuery);

// $.telligent.sharepoint.widgets.editDocument
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        contentId: null,
        tags: [],
        tagBox: ".tag-box",
        selectTags: ".select-tag-box",
        saveButtonId: null,
        saveUrl: null,
        deleteButtonId: null,
        deleteUrl: null
    },
    $saveButton,
    $deleteButton,
    attachHandlers = function (context) {
        // Tags
        $(context.tagBox, context.wrapper).evolutionTagTextBox({ allTags: context.tags });
        var $selectTagsBox = $(context.selectTags, context.wrapper).click(function (e) {
            e.preventDefault();
            $(context.tagBox, context.wrapper).evolutionTagTextBox('openTagSelector');
        });
        if (!context.tags || context.tags.length === 0) {
            $selectTagsBox.hide();
        }

        // Save
        $saveButton = $(context.saveButtonId, context.wrapper).click(function (e) {
            if (context.__saving || context.__deleting) return false;

            if ($(this).evolutionValidation('isValid')) {
                save(context);
            }
            return false;
        });
        $saveButton.evolutionValidation({
            validateOnLoad: true,
            onValidated: function (isValid, buttonClicked, c) {
                if (isValid) {
                    $saveButton.removeClass('disabled');
                } else {
                    $saveButton.addClass('disabled');
                }
            },
            onSuccessfulClick: function (e) {
                $saveButton.addClass('disabled').parent().find('.processing').show();
            }
        });
        context.validateForm = $saveButton.evolutionValidation('addCustomValidation', 'list-item-fields', function () {
            return !context.__deleting && $.telligent.sharepoint.widgets.validator.validateAndGetFormData(context.wrapper).isValid;
        }, '', null, context.wrapper);

        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register($saveButton);

        setInterval(function () {
            context.validateForm();
        }, 2500);

        // Delete
        $deleteButton = $(context.deleteButtonId, context.wrapper).click(function (e) {
            e.preventDefault();
            if (context.__saving || context.__deleting) return false;

            context.__deleting = true;
            $saveButton.addClass('disabled');
            $deleteButton.addClass('disabled').parent().find('.processing').show();
            $("fieldset.edit-document", context.wrapper).attr('disabled', 'disabled');
            $.telligent.evolution.del({
                url: context.deleteUrl,
                success: function (response) {
                    if (response && response.valid) {
                        window.location = context.libraryUrl;
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
                    context.__deleting = false;
                    $("fieldset.edit-document", context.wrapper).removeAttr('disabled');
                }
            });
        });
        $.telligent.evolution.navigationConfirmation.register($deleteButton);
    },
    save = function (context) {
        var saveText = $saveButton.text(),
            data = getFormData(context);
        if (!data) return;

        context.__saving = false;
        $saveButton.addClass("disabled").text($saveButton.attr('disabled-text'));
        $deleteButton.addClass('disabled');
        $("fieldset.edit-document", context.wrapper).attr('disabled', 'disabled');
        $.telligent.evolution.post({
            url: context.saveUrl,
            data: data,
            success: function (response) {
                if (response && response.valid) {
                    window.location = response.redirectUrl || context.libraryUrl;
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
                $saveButton.removeClass("disabled").parent().find('.processing').hide();
                $deleteButton.removeClass('disabled');
                $("fieldset.edit-document", context.wrapper).removeAttr('disabled');
            }
        });
    },
    getFormData = function (context) {
        var data = {
            DocumentId: context.contentId
        };

        // append list-item fields values
        var formData = $.telligent.sharepoint.widgets.validator.validateAndGetFormData(context.wrapper);
        if (!formData.isValid) return false;
        $.extend(data, formData.data);

        // append data with tags
        var inTags = $(context.tagBox, context.wrapper).val().split(/[,;]/g);
        var tags = [];
        for (var i = 0; i < inTags.length; i++) {
            var tag = $.trim(inTags[i]);
            if (tag) {
                tags[tags.length] = tag;
            }
        }
        tags = tags.join(',');
        data.Tags = tags;

        return data;
    };

    $.telligent.sharepoint.widgets.editDocument = {
        register: function (context) {
            attachHandlers(context);
        }
    };

})(jQuery, window);

// $.telligent.sharepoint.widgets.uploadDocument
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var context,
    defaultOptions = {
        wrapper: document,
        libraryId: null,
        relatedFoldersUrl: null,
        foldersUrl: null,
        currentFolder: null,
        attachmentId: null,
        attachmentProgressText: 'Uploading ({0}%)...',
        attachmentChangeText: 'Change',
        attachmentAddText: 'Upload',
        previewAttachmentUrl: null,
        uploadContextId: null,
        uploadFileUrl: null,
        documentNameId: null,
        overwriteId: null,
        tagBox: ".tag-box",
        selectTagsBox: "",
        tags: [],
        saveButtonId: null,
        saveUrl: null,
        invalidFileNameText: "The file name is not allowed.",
        noFileUploadedError: "No file uploaded.",
        noFileNameError: "No file name.",
        libraryUrl: null
    },
    fileNameForbiddenCharacters = /(^\.|\.$)|(\.\.+)|[~"#%&*:<>\?\/\\\{|\}]/ig,
    $saveButton,
    attachHandlers = function (context) {
        // Tags
        $(context.tagBox, context.wrapper).evolutionTagTextBox({ allTags: context.tags });
        $(context.selectTagsBox, context.wrapper).click(function () {
            $(context.tagBox, context.wrapper).evolutionTagTextBox('openTagSelector');
            return false;
        });
        if (!context.tags || context.tags.length === 0) {
            $(context.selectTagsBox, context.wrapper).hide();
        }

        // Save
        $saveButton = $(context.saveButtonId, context.wrapper).click(function (e) {
            e.preventDefault();
            if (!$(this).evolutionValidation('isValid')) {
                return;
            }
            save(context);
        });
        $saveButton.evolutionValidation({
            validateOnLoad: false,
            onValidated: function (isValid, buttonClicked, c) {
                if (isValid) {
                    $saveButton.removeClass('disabled');
                } else {
                    $saveButton.addClass('disabled');
                }
            },
            onSuccessfulClick: function (e) {
                $saveButton.addClass('disabled').parent().find('.processing').show();
            }
        });
        context.validateFile = $saveButton.evolutionValidation('addCustomValidation', 'document-attachment-uploaded', function () {
            return context.file && context.file.fileName && context.file.fileName.length > 0;
        },
        context.noFileUploadedError,
        '.field-item.post-attachment .field-item-validation',
        context.wrapper);

        context.validateFileName = $saveButton.evolutionValidation('addCustomValidation', 'document-file-name', function () {
            var fileName = $(context.documentNameId, context.wrapper).val();
            if (!fileName || fileName.length === 0) {
                context.__error = context.noFileNameError;
                return false;
            }
            if (fileNameForbiddenCharacters.test(fileName)) {
                context.__error = context.invalidFileNameError;
                return false;
            }
            return true;
        },
        context.__error || context.invalidFileNameError,
        '.field-item.destination-file-name .field-item-validation',
        context.wrapper);

        $.telligent.evolution.navigationConfirmation.enable();
        $.telligent.evolution.navigationConfirmation.register($saveButton);

        // Attachments
        context.attachment = $(context.attachmentId);
        context.attachmentUpload = context.attachment.find('a.upload');
        context.attachmentRemove = context.attachment.find('a.remove');
        context.attachmentName = context.attachment.find('input');
        context.attachmentPreview = context.attachment.find('.preview');

        context.attachmentRemove.click(function () {
            context.file = null;
            context.attachmentName.val('');
            context.attachmentUpload.html(context.attachmentAddText).removeClass('change').addClass('add')
            if (context.attachment.data('link') == 'True') {
                context.attachmentName.removeAttr('readonly');
            }
            context.attachmentRemove.hide();
            context.validateFile();
            loadPreview(context);
            return false;
        });

        context.attachmentUpload.glowUpload({
            fileFilter: null,
            uploadUrl: context.uploadFileUrl,
            renderMode: 'link'
        })
        .bind('glowUploadBegun', function (e) {
            context.uploading = true;
            context.attachmentUpload.html(context.attachmentProgressText.replace('{0}', 0));
        })
        .bind('glowUploadComplete', function (e, file) {
            if (file && file.name.length > 0) {
                context.file = {
                    fileName: file.name,
                    isRemote: false,
                    isNew: true
                };
                context.attachmentName.val(context.file.fileName).attr('readonly', 'readonly');
                context.validateFile();
                loadPreview(context);
                context.uploading = false;
                context.attachmentUpload.html(context.attachmentChangeText).removeClass('add').addClass('change');
                context.attachmentRemove.show();
                context.validateFile();

                // change a destination file name
                $(context.documentNameId, context.wrapper).val(context.file.fileName);
            }
        })
        .bind('glowUploadProgress', function (e, details) {
            context.attachmentUpload.html(context.attachmentProgressText.replace('{0}', details.percent));
        });

        $(context.documentNameId, context.wrapper).on('keyup change', function () {
            context.validateFileName();
        });
    },
    save = function (context) {
        var data = getFormData(context);
        if (!data) return;

        $.telligent.evolution.put({
            url: context.saveUrl,
            data: data,
            success: function (response) {
                if (response && response.valid) {
                    if (response.redirectUrl) {
                        window.location = response.redirectUrl;
                    }
                    else {
                        if (data.DestinationFolder && data.DestinationFolder.length > 0) {
                            context.libraryUrl += "#" + data.DestinationFolder;
                        }
                        window.location = context.libraryUrl;
                    }
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
                $saveButton.removeClass("disabled").parent().find('.processing').hide();
            }
        });
    },
    getFormData = function (context) {
        var folderPath = $.telligent.sharepoint.widgets.documentFoldersTree.get_folderPath(context) || context.currentFolder,
        data = {
            DestinationFileName: $(context.documentNameId, context.wrapper).val(),
            DestinationFolder: folderPath,
            FileContextId: context.uploadContextId,
            FileName: context.file.fileName,
            LibraryId: context.libraryId,
            Overwrite: $(context.overwriteId, context.wrapper).attr("checked") === "checked"
        };

        // append list-item fields values
        var formData = $.telligent.sharepoint.widgets.validator.validateAndGetFormData($(".list-item-field", context.wrapper));
        if (!formData.isValid) return false;
        $.extend(data, formData.data);

        // append data with tags
        var inTags = $(context.tagBox, context.wrapper).val().split(/[,;]/g);
        var tags = [];
        for (var i = 0; i < inTags.length; i++) {
            var tag = $.trim(inTags[i]);
            if (tag) {
                tags[tags.length] = tag;
            }
        }
        tags = tags.join(',');
        data.Tags = tags;

        return data;
    },
    loadPreview = function (context) {
        if (context.file && (context.file.fileName || context.file.url)) {
            clearTimeout(context.attachmentPreviewTimeout);
            context.attachmentPreviewTimeout = setTimeout(function () {
                var data = {
                    w_uploadContextId: context.uploadContextId
                };
                if (context.file.url) {
                    data.w_url = context.file.url;
                }
                if (context.file.fileName) {
                    data.w_filename = context.file.fileName;
                }
                $.telligent.evolution.post({
                    url: context.previewAttachmentUrl,
                    data: data,
                    success: function (response) {
                        response = $.trim(response);
                        if (response && response.length > 0 && response !== context.attachmentPreviewContent) {
                            context.attachmentPreviewContent = response;
                            context.attachmentPreview.html(context.attachmentPreviewContent).removeClass('empty');
                        }
                    }
                });
            }, 150);
        } else {
            context.attachmentPreviewContent = '';
            context.attachmentPreview.html('').addClass('empty');
        }
    };

    $.telligent.sharepoint.widgets.uploadDocument = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
            $.telligent.sharepoint.widgets.documentFoldersTree.register(context);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.documentFoldersTree
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        relatedFoldersUrl: null,
        foldersUrl: null,
        rootFolder: '/',
        currentFolder: null
    },
    speed = 100,
    $folderTemplate,
    $folderTree,
    init = function (context) {
        $folderTemplate = $("script[type='folder-children-template']", context.wrapper);
        $folderTree = $('.folder-root', context.wrapper);
        if (!context.currentFolder) {
            context.currentFolder = window.location.hash.split('#')[1] || context.rootFolder;
        }
        save(context);
        loadRelatedFolders(context, context.currentFolder, function () {
            $folderTree.addClass("loading");
        }, function (response) {
            if (response && response.levels) {
                var $folders = $(buildHtml(response.levels)).hide();
                $folderTree.html($folders).removeClass("loading");
                $folders.slideDown(speed);
                // expand subfolders for a selected folder
                $(".folder.selected>a>.expand-collapse.haschilds", $folderTree).click();
            }
        }, function () {
            $folderTree.removeClass("loading");
        });
    },
    loadRelatedFolders = function (context, folderPath, before, success, complete) {
        if (typeof before === "function") before();
        $.telligent.evolution.get({
            url: context.relatedFoldersUrl,
            data: {
                w_folder: folderPath
            },
            success: success,
            complete: complete
        });
        if (typeof after === "function") after();
    },
    expandSubfolders = function (context, folderPath, holder) {
        var $folderItem = $(holder).closest(".folder-item").addClass("loading");
        $.telligent.evolution.get({
            url: context.foldersUrl,
            data: {
                w_folder: folderPath
            },
            success: function (response) {
                var $subfolders = $(buildHtml([{
                    folders: response.folders
                }]));
                $subfolders.hide();
                $(holder).replaceWith($subfolders);
                $subfolders.slideDown(speed);
                $folderItem.removeClass("loading").find(".expand-collapse.haschilds").first().addClass("expanded");
            }
        });
    },
    buildHtml = function (folderLevels) {
        if (typeof folderLevels === "undefined" || folderLevels.length == 0) return '';
        // create html DOM from template
        var $template = $($folderTemplate.html()),
            // find a for-each template, remove attributes and cache it
            $foreachTemplate = $('[for-each]', $template).removeAttr('for-each'),
            // store html template for an individual item as text
            itemTemplateHtml = $foreachTemplate.html(),
            // apply template to the folder level with a specified index
            applyTemplate = function (folderLevels, index) {
                if (typeof index === "undefined" || typeof index !== "number") index = 0;
                if (index >= folderLevels.length || !folderLevels[index].folders) return '';
                var innerHtml = '',
                    folders = folderLevels[index].folders || [],
                    folder,
                    folderHtml;
                for (var i = 0, len = folders.length; i < len; i++) {
                    folder = folders[i];
                    folderHtml = itemTemplateHtml;
                    for (var property in folder) {
                        if (typeof folder[property] !== 'function') {
                            var textValue = folder[property];
                            if (typeof textValue === "boolean") {
                                textValue = folder[property] ? property : '';
                            }
                            folderHtml = folderHtml.replace(new RegExp('{{' + property + '}}', 'g'), textValue);
                        }
                    }
                    if (folder.haschilds && folder.expanded) {
                        folderHtml = folderHtml.replace('{{childs}}', outerHTML(applyTemplate(folderLevels, index + 1)));
                    }
                    else if (folder.haschilds) {
                        folderHtml = folderHtml.replace('{{childs}}', "<subfolders data-path='" + folder.path + "'></subfolders>");
                    }
                    else {
                        folderHtml = folderHtml.replace('{{childs}}', '');
                    }
                    innerHtml += folderHtml;
                }
                // save changes in DOM
                $foreachTemplate.html(innerHtml);
                return $template[0];
            },
            outerHTML = function (node) {
                if (typeof node === "object") {
                    return node.outerHTML || new XMLSerializer().serializeToString(node);
                }
                return node;
            };
        return applyTemplate(folderLevels, 0);
    },
    attachHandlers = function (context) {
        $(context.wrapper).on("click", ".folder-item .expand-collapse.haschilds", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $subfolders = $(this).closest(".folder-item").children("subfolders");
            if ($subfolders && $subfolders.length > 0) {
                expandSubfolders(context, $(this).data("path"), $subfolders);
            }
            else {
                if ($(this).hasClass('expanded')) {
                    $(this).removeClass('expanded').addClass('collapsed');
                    $(this).closest(".folder-item").children(".folder-children").slideUp(speed);
                }
                else {
                    $(this).addClass('expanded').removeClass('collapsed');
                    $(this).closest(".folder-item").children(".folder-children").slideDown(speed);
                }
            }
        }).on("click", ".folder>a", function (e) {
            context.currentFolder = $(this).closest("[data-path]").data("path");
            save(context);
            $(".folder", context.wrapper).removeClass("selected");
            $(this).closest(".folder").addClass("selected");
            context.handled = true;
        });
        $(global).bind("hashchange", function () {
            if (!context.handled) {
                var folderPath = global.location.hash.split('#')[1] || '/';
                context.currentFolder = folderPath;
                save(context);
                var $folderItem = $(".folder-item[data-path='" + folderPath + "']", context.wrapper);
                if ($folderItem && $folderItem.length > 0) {
                    $(".folder", context.wrapper).removeClass("selected");
                    $folderItem.children(".folder").addClass("selected");
                    var $parentFolderItem = $folderItem.parent().closest(".folder-item");
                    $parentFolderItem.find(".expand-collapse").first().addClass("expanded").removeClass('collapsed');
                    $parentFolderItem.children(".folder-children").slideDown(speed);
                }
                else {
                    loadRelatedFolders(context, folderPath, function () {
                        $folderTree.addClass("loading");
                    }, function (response) {
                        if (response && response.levels) {
                            var $folders = $(buildHtml(response.levels)).hide();
                            $folderTree.html($folders).removeClass("loading");
                            $folders.show(speed);
                        }
                    }, function () {
                        $folderTree.removeClass("loading");
                    });
                }
            }
            context.handled = false;
        });
    },
    save = function (context) {
        $(context.wrapper).data('folderPath', context.currentFolder);
    },
    load = function (context) {
        return $(context.wrapper).data('folderPath');
    };

    $.telligent.sharepoint.widgets.documentFoldersTree = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        },
        get_folderPath: function (context) {
            return load(context);
        }
    };
})(jQuery, window);