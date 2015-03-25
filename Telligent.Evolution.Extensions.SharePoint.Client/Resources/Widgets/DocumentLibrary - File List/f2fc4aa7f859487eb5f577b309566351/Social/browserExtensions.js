(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        DocumentCouldNotBeOpenedError: "The document could not be opened for editing. A compatible application could not be found to edit the document.",
        NoCompatibleAppError: "'Edit Document' requires a compatible application and web browser."
    },
    code = {
        couldNotBeOpened: 1,
        noCompatibleApp: 2,
        ok: 0
    };
    OpenDoc = null,
    UnsupportedExtensions = ["ascx", "asp", "aspx", "htm", "html", "master", "odc", "exe", "bat", "com", "cmd", "onetoc2"],
    editDocument = function (context, fileName) {
        var result = editDocumentWithPluginOrActiveX(fileName);
        var error = "";
        switch (result) {
            case code.couldNotBeOpened:
                error = context.DocumentCouldNotBeOpenedError;
                break;
            case code.noCompatibleApp:
                error = context.NoCompatibleAppError;
                break;
        }
        return {
            hasErrors: result !== code.ok,
            error: error
        };
    },
    editDocumentWithPluginOrActiveX = function (fileName) {
        if (fileName == null || fileName.length === 0) return code.couldNotBeOpened;

        var browserExtension,
            progID = "",
            useLocalCopy = false,
            openDocuments = "SharePoint.OpenDocuments";

        if (fileName.charAt(0) == "/" || fileName.substr(0, 3).toLowerCase() == "%2f") {
            fileName = document.location.protocol + "//" + document.location.host + fileName
        }

        var fileExtension = createExtension(decodeURIComponent(fileName));
        if (!isSupportCheckoutToLocal(fileExtension)) {
            return code.couldNotBeOpened
        }

        try {
            browserExtension = ensureExtension(openDocuments + ".3");
            if (browserExtension != null) {
                var isOpened = browserExtension.EditDocument3(window, fileName, useLocalCopy, progID);
                if (!isOpened) {
                    return code.couldNotBeOpened;
                }
                return code.ok;
            }
        }
        catch (e) { }

        try {
            browserExtension = ensureExtension(openDocuments);
            if (browserExtension != null) {
                try {
                    var isOpened = browserExtension.EditDocument2(window, fileName, progID);
                    if (!isOpened) {
                        return code.couldNotBeOpened;
                    }
                    return code.ok;
                }
                catch (e) { }

                try {
                    if (extension === "ppt" && progID === "") {
                        progID = "PowerPoint.Slide";
                    }
                    var isOpened = browserExtension.EditDocument(fileName, progID);
                    if (!isOpened) {
                        return code.couldNotBeOpened;
                    }
                    return code.ok;
                }
                catch (e) { }
                return code.noCompatibleApp;
            }
        }
        catch (e) { }

        return code.couldNotBeOpened;
    },
    createExtension = function (fileName) {
        var name = new String(fileName),
            extensionPattern = /^.*\.([^\.]*)$/;
        return name.replace(extensionPattern, "$1").toLowerCase();
    },
    isSupportCheckoutToLocal = function (extension) {
        if (extension == null || extension == "")
            return false;

        extension = extension.toLowerCase();
        for (var i = 0, len = UnsupportedExtensions.length; i < len; i++) {
            if (extension == UnsupportedExtensions[i]) {
                return false;
            }
        }
        return true;
    },
    ensureExtension = function (a) {
        if (OpenDoc == null) {
            var isIE11 = !!navigator.userAgent.match(/Trident.*rv[ :]*11\./);
            var plugin = navigator.plugins["OpenDocuments"];
            if (plugin != null) {
                OpenDoc = plugin;
            }
            else if (isIE11 || typeof window.ActiveXObject !== "undefined") {
                try {
                    OpenDoc = new ActiveXObject(a);
                }
                catch (c) { }
            }
            else {
                try {
                    OpenDoc = createSharePointPlugin();
                }
                catch (c) { }
            }
        }
        return OpenDoc;
    },
    createSharePointPlugin = function () {
        var pluginElement = document.getElementById("SharePointPlugin");
        if (!pluginElement && isSharePointPluginInstalled()) {
            var o = document.createElement("object");
            o.id = "SharePointPlugin";
            o.type = "application/x-sharepoint";
            o.width = 0;
            o.height = 0;
            o.style.setProperty("visibility", "hidden", "");
            document.body.appendChild(o);
            pluginElement = document.getElementById("SharePointPlugin");
        }
        return pluginElement;
    },
    isSharePointPluginInstalled = function () {
        return navigator.mimeTypes && navigator.mimeTypes['application/x-sharepoint'] && navigator.mimeTypes['application/x-sharepoint'].enabledPlugin;
    };

    $.telligent.sharepoint.widgets.browserExtensions = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            return {
                editDocument: function (fileName) {
                    return editDocument(context, fileName);
                }
            };
        }
    }

})(jQuery, window);