(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,
        relatedFoldersUrl: null,
        foldersUrl: null,
        currentFolder: null
    },
    apiEvent = {
        updated: 'documentLibraryUpdated'
    },
    speed = 100,
    $folderTemplate,
    $folderTree,
    init = function (context) {
        $folderTemplate = $("script[type='folder-children-template']", context.wrapper);
        $folderTree = $('.folder-root', context.wrapper);
        loadRelatedFolders(context, context.currentFolder || window.location.hash.split('#')[1] || '/', function () {
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
            complete: complete,
            error: function (xhr, desc, ex) {
                if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                    $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                }
                else {
                    $.telligent.evolution.notifications.show(desc, { type: 'error' });
                }
            }
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
            },
            error: function (xhr, desc, ex) {
                if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                    $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                }
                else {
                    $.telligent.evolution.notifications.show(desc, { type: 'error' });
                }
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
            $(".folder", context.wrapper).removeClass("selected");
            $(this).closest(".folder").addClass("selected");
            context.handled = true;
        });
        $(global).on("hashchange", function () {
            if (!context.handled) {
                var folderPath = window.location.hash.split('#')[1] || '/';
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
        }).on(apiEvent.updated, function (e, documentLibraryRemovedEventArgs) {
            var folderPath = window.location.hash.split('#')[1] || '/';
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
        });
    };

    $.telligent.sharepoint.widgets.foldersTree = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);