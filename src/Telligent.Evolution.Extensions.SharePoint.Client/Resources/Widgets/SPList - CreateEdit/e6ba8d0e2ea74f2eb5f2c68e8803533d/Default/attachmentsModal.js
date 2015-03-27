(function ($) {
    window.console = window.console || { log: function () { } };
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.listItem = $.telligent.sharepoint.widgets.listItem || {};

    var init = function (context) {
        $(context.fileUploaderId).glowMultiUpload({
            uploadUrl: context.uploadFileUrl,
            autoUpload: true,
            width: '100%',
            height: '150px'
        });
    },
    attachHandlers = function (context) {
        $(context.saveHolderId).click(function (e) {
            e.preventDefault();
            var files = $(context.fileUploaderId).glowMultiUpload('val');
            window.parent.jQuery.glowModal.opener(window).jQuery.glowModal.close(files);
        });

        $(context.cancelHolderId).click(function (e) {
            e.preventDefault();
            window.parent.jQuery.glowModal.opener(window).jQuery.glowModal.close();
        });
    };
    $.telligent.sharepoint.widgets.listItem.attachments = {
        register: function (context) {
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery);
