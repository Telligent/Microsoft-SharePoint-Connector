(function ($, global) {

    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        rootFolder: null,
        folderPath: null,
        libraryTitleHolder: null,
        redirectUrl: null,
        defaultTitle: ''
    },
    publicEvents = {
        documentLibraryUpdated: 'documentLibraryUpdated',
        documentLibraryAdded: 'documentLibraryAdded',
        documentLibraryRemoved: 'documentLibraryRemoved'
    },
    attachHandlers = function (context) {
        $(context.wrapper).on("click", ".open-folder[url]", function (e) {
            if (context.redirectUrl && context.redirectUrl.length > 0) {
                return true;
            }

            window.location.hash = $(this).attr("url");
            return false;
        });

        $(global).bind(publicEvents.documentLibraryUpdated, function (e, args) {
            buildBreadCrumbs(args.libraryRootFolder, args.folderPath, context);
        }).bind(publicEvents.documentLibraryAdded, function (e, args) {
            if (args.count === 1) {
                buildBreadCrumbs(args.library.root, args.library.root, context);
            } else {
                $(context.libraryTitleHolder, context.wrapper).text(context.defaultTitle);
                $(context.wrapper).html('');
            }
        }).bind(publicEvents.documentLibraryRemoved, function (e, args) {
            if (args.count === 1) {
                buildBreadCrumbs(args.library.root, args.library.root, context);
            } else {
                $(context.libraryTitleHolder, context.wrapper).text(context.defaultTitle);
                $(context.wrapper).html('');
            }
        });
    },
    buildBreadCrumbs = function (rootFolder, folderPath, context) {
        var folderTree = folderPath.replace(/^\/+/, "").replace(/\/+$/, "").split("/");
        var folderTreeRoot = rootFolder.replace(/^\/+/, "").replace(/\/+$/, "").split("/");

        if (folderTreeRoot.length >= 1) {
            var libraryTitle = folderTreeRoot[folderTreeRoot.length - 1];
            context.libraryTitleHolder.text(libraryTitle);
        }

        if (folderTree.length == 0) {
            folderTree.push(folderTreeRoot);
        }

        var $tempHtmlHolder = $('<div></div>');
        var path = '';
        var rootFolderLength = folderTreeRoot.length > 0 ? folderTreeRoot.length : 0;
        for (var i = 0, len = folderTree.length; i < len; i++) {
            path = [path, '/', folderTree[i]].join('');
            var isRootFolderPath = (i < rootFolderLength - 1);
            if (isRootFolderPath) continue;

            var $temp = context.template.clone().attr("id", "").attr("style", "");
            var $links = $temp.find("a.open-folder");
            var lastCrumb = i == len - 1;
            var currentFolder = lastCrumb ? "" : path;

            if (context.redirectUrl && context.redirectUrl.length > 0 && !lastCrumb) {
                var href = [context.redirectUrl, "#", currentFolder].join('');
                $links.attr("href", href);
            }
            else if (!lastCrumb) {
                $links.attr("url", currentFolder);
            }

            $links.text(folderTree[i]);

            $tempHtmlHolder.append($temp.html());
        }
        $(context.wrapper).html($tempHtmlHolder.html());
    };

    $.telligent.sharepoint.widgets.sharePointBreadcrumbs = {
        register: function (context) {
            var options = $.extend({}, defaultOptions, context);
            if (options.rootFolder && options.folderPath) {
                buildBreadCrumbs(options.rootFolder, options.folderPath, options);
            }
            attachHandlers(options);
        }
    };
})(jQuery, window);
