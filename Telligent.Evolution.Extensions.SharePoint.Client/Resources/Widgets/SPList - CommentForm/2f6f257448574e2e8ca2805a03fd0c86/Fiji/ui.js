(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.evolution = $.telligent.evolution || {};
    $.telligent.evolution.sharepoint = $.telligent.evolution.sharepoint || {};
    $.telligent.evolution.sharepoint.widgets = $.telligent.evolution.sharepoint.widgets || {};
    $.telligent.evolution.sharepoint.widgets.listItem = $.telligent.evolution.sharepoint.widgets.listItem || {};

    var MAX_COMMENT_LENGTH = 1000000;

    var widgetUI = {
        wrapper: null,
        body: null,
        save: null,
        successMessage: null,
        moderateMessage: null,
        errorMessage: null
    },
    widgetText = {
        publishingText: "",
        publishErrorText: "",
        publishText: "",
        bodyRequiredText: ""
    },
    widgetURLs = {
        addCommentURL: ""
    },
    widgetData = {
        contentId: null,
        contentTypeId: null
    };

    var events = {
        commentPosted: function (contentId, comment) {
            $(document).trigger('sharepoint_listItem_commentPosted', {
                contentId: contentId,
                comment: comment
            });
        }
    };

    var init = function (context) {
        var __copyProperties = function (source, dest) {
            for (var i in dest) {
                if (dest.hasOwnProperty(i) && source.hasOwnProperty(i)) {
                    dest[i] = source[i];
                }
            }
        };
        __copyProperties(context, widgetUI);
        __copyProperties(context, widgetText);
        __copyProperties(context, widgetURLs);
        __copyProperties(context, widgetData);
    },
    attachHandlers = function () {
        if (document.URL.indexOf('#addcomment') >= 0) {
            widgetUI.body.focus();
        }

        $('.internal-link.close-message', widgetUI.wrapper).click(function () {
            $(this).blur();
            widgetUI.successMessage.fadeOut().slideUp();
            return false;
        });

        widgetUI.body.one('focus', function () {
            widgetUI.body.evolutionComposer({
                plugins: ['mentions', 'hashtags']
            });
        });

        widgetUI.save.evolutionValidation({
            onValidated: function (isValid, buttonClicked, c) {
                if (isValid) {
                    widgetUI.save.removeClass('disabled');
                } else {
                    widgetUI.save.addClass('disabled');
                }
            },
            onSuccessfulClick: function (e) {
                e.preventDefault();
                $('.processing', widgetUI.save.parent()).css("visibility", "visible");
                widgetUI.save.addClass('disabled');
                save();
            }
        }).evolutionValidation('addField', widgetUI.body, {
            required: true,
            maxlength: MAX_COMMENT_LENGTH,
            messages: {
                required: widgetText.bodyRequiredText
            }
        }, '#' + widgetUI.wrapper.attr('id') + ' .field-item.post-body .field-item-validation', null);
    },
    save = function () {
        widgetUI.successMessage.hide();
        widgetUI.moderateMessage.hide();
        widgetUI.errorMessage.hide();

        widgetUI.save.html('<span></span>' + widgetText.publishingText).addClass('disabled');

        $.telligent.evolution.post({
            url: widgetURLs.addCommentURL,
            data: {
                Comment: widgetUI.body.evolutionComposer('val'),
                ContentId: widgetData.contentId,
                ContentTypeId: widgetData.contentTypeId
            },
            success: function (response) {
                $('.processing', widgetUI.wrapper).css('visibility', 'hidden');
                widgetUI.successMessage.slideDown();
                global.setTimeout(function () { widgetUI.successMessage.fadeOut().slideUp(); }, 9999);

                events.commentPosted(widgetData.contentId, widgetUI.body.evolutionComposer('val'));

                widgetUI.body.evolutionComposer('val', '');
                widgetUI.body.change();
                widgetUI.save.evolutionValidation('reset');
                widgetUI.save.html('<span></span>' + widgetText.publishText).removeClass('disabled');
            },
            error: function (xhr, desc, ex) {
                $('.processing', widgetUI.wrapper).css("visibility", "hidden");
                widgetUI.save.html('<span></span>' + widgetText.publishText).removeClass('disabled');
                widgetUI.errorMessage.html(widgetText.publishErrorText + ' (' + desc + ')').slideDown();
            }
        });
    };

    $.telligent.evolution.sharepoint.widgets.listItem.commentCreate = {
        register: function (context) {
            init(context);
            attachHandlers();
        }
    };

})(jQuery, window);
