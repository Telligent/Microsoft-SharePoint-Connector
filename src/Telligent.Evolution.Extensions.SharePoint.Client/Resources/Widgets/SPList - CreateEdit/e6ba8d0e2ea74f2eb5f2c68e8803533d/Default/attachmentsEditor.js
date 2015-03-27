(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapperId: document,
        htmlTemplateId: null,
        uploadId: null,
        uploadModalUrl: null,
        uploadModalWidth: 500,
        uploadModalHeight: 300,
        addedHiddenId: null,
        removedHiddenId: null
    },
    mergeArrays = function (source, dest) {
        var resultSet = source.length > 0 ? source.slice(0) : [];
        for (var index = 0, length = dest.length; index < length; index++) {
            if (source.indexOf(dest[index]) == -1) {
                resultSet.push(dest[index]);
            }
        }
        return resultSet;
    },
    init = function (context) {
        context.$addedHidden = $(context.addedHiddenId);
        context.get_added = function () {
            var addedItemsRow = this.$addedHidden.val();
            return addedItemsRow && addedItemsRow.length > 0 ? addedItemsRow.split(';') : [];
        };
        context.commit_added = function (added) {
            var result = added && added.length > 0 ? added.join(';') : '';
            this.$addedHidden.val(result);
        };
        context.$removedHidden = $(context.removedHiddenId);
        context.get_removed = function () {
            var removedItemsRow = this.$removedHidden.val();
            return removedItemsRow && removedItemsRow.length > 0 ? removedItemsRow.split(';') : [];
        };
        context.commit_removed = function (removed) {
            var result = removed && removed.length > 0 ? removed.join(';') : '';
            this.$removedHidden.val(result);
        };
    },
    attachHandlers = function (context) {
        $(context.uploadId).click(function (e) {
            e.preventDefault();
            $.glowModal(context.uploadModalUrl, {
                width: context.uploadModalWidth,
                height: context.uploadModalHeight,
                onClose: function (files) {
                    if (files && files.length > 0) {
                        var fileNames = [];
                        for (var i = 0, len = files.length; i < len; i++) {
                            fileNames.push(files[i].name);
                        }
                        context.commit_added(mergeArrays(fileNames, context.get_added()));
                        renderNewAttachments(context, fileNames);
                    }
                }
            });
        });
        $(context.wrapperId).on('click', ".attachment-item[data-name] .remove", function (e) {
            e.preventDefault();
            var $attachment = $(this).closest('.attachment-item[data-name]'),
                removed = [$attachment.data('name')];
            if ($attachment.hasClass("added")) {
                var added = context.get_added(),
                    index = added.indexOf(removed[0]);
                if (index !== -1) {
                    added.splice(index, 1);
                    context.commit_added(added);
                }
            }
            else {
                context.commit_removed(mergeArrays(removed, context.get_removed()));
            }
            $attachment.remove();
        });
    },
    renderNewAttachments = function (context, added) {
        for (var i = 0, len = added.length; i < len; i++) {
            var fileName = added[i];
            if (fileName && fileName.length > 0) {
                createHtmlElement(context, fileName);
            }
        }
    },
    createHtmlElement = function (context, fileName) {
        var $attachment = $(context.htmlTemplateId).clone().attr('id', '').attr('data-name', fileName);
        $attachment.find('.attachment').text(fileName);
        $(context.htmlTemplateId).before($attachment);
        $attachment.show();
    };

    $.telligent.sharepoint.widgets.attachmentsEditor = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery);