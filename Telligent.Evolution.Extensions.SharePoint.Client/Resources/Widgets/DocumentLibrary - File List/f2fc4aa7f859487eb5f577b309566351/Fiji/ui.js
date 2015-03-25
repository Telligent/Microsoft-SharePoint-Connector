(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
    $.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

    String.prototype.updateQueryString = function (key, value) {
        var uri = this;
        var keyPattern = new RegExp("([?|&])" + key + "=.*?(&|$)", "i");
        var separator = uri.indexOf('?') === -1 ? "?" : "&";
        if (uri.match(keyPattern)) {
            return uri.replace(keyPattern, ['$1', key, "=", value, '$2'].join(''));
        }
        else {
            return [uri, separator, key, "=", value].join('');
        }
    };

    String.prototype.removeQueryStringKey = function (key) {
        var uri = this;
        var keyPattern = new RegExp("([?|&])" + key + "=.*?(&|$)", "i");
        if (uri.match(keyPattern)) {
            uri = uri.replace(keyPattern, '$1');
            var lastChar = uri[uri.length - 1];
            if (lastChar === '?' || lastChar == '&') {
                uri = uri.slice(0, -1);
            }
        }
        return uri;
    };

    var defaultOptions = {
        widget: null,
        visible: false,

        contentHolderId: '',
        libraryId: null,
        libraryRootFolder: null,
        sortBy: '',
        sortOrder: 'Ascending',
        page: 0,

        view: 'ExplorerView',
        listViewName: 'ListView',
        listViewUrl: null,
        explorerViewName: 'ExplorerView',
        explorerViewUrl: null,

        updateUrl: null,

        createFolderWidth: 500,
        createFolderHeight: 300,
        createFolderUrl: null,

        uploadDocumentWidth: 500,
        uploadDocumentHeight: 300,
        uploadDocumentUrl: null,

        versionsModalWidth: 780,
        versionsModalHeight: 300,
        versionsModalUrl: null,

        permissionsModalWidth: 500,
        permissionsModalHeight: 300,
        permissionsModalUrl: null,

        callTimeOut: 60000
    },
    api = {
        update: 'update',
        rename: 'rename',
        refresh: 'refresh',
        versions: 'versions',
        permissions: 'permissions'
    },
    documentLibrariesAdministrationEvent = {
        added: 'documentLibraryAdded',
        removed: 'documentLibraryRemoved'
    },
    documentLibraryEvent = {
        updated: 'documentLibraryUpdated'
    },
    init = function (context) {
        var documentLibrariesChangedEventHandler = function (ctx, librariesCount, library) {
            if (librariesCount == 1) {
                ctx.widget.show();
                ctx.libraryId = library.id;
                ctx.libraryRootFolder = library.root;
                refresh(defaultViewUrl(ctx), ctx);
            }
            else {
                ctx.widget.hide();
            }
        };

        if (context.visible) {
            context.widget.show();
            folderPath = window.location.hash.split('#')[1];
            if (!!folderPath) {
                context.folderPath = folderPath;
            }
            refresh(defaultViewUrl(context), context);
        } else {
            context.widget.hide();
        }

        $(global).bind(documentLibrariesAdministrationEvent.added, function (e, documentLibraryAddedEventArgs) {
            documentLibrariesChangedEventHandler(context, documentLibraryAddedEventArgs.count, documentLibraryAddedEventArgs.library);
        }).bind(documentLibrariesAdministrationEvent.removed, function (e, documentLibraryRemovedEventArgs) {
            if (documentLibraryRemovedEventArgs.count == 0) {
                context.widget.hide();
            }
        });
    },
    fileSystemAPIsSupported = window.File && window.FileReader && window.FileList && window.Blob,
    dropBoxDefaultHtml,
    showDropBox = function (context) {
        var left = 0,
            top = 0,
            width = 0,
            height = 0,
            $files = $('ul.file-list>li', context.widget),
            firstFilePosition = $files.first().position(),
            lastFilePosition = $files.last().position(),
            paddingTop = parseInt($files.first().css("padding-top"), 10);
        left = firstFilePosition.left;
        top = firstFilePosition.top;
        if (context.view === "ListView") {
            width = $files.width();
        }
        else {
            width = $('ul.file-list', context.widget).width();
        }
        height = lastFilePosition.top - firstFilePosition.top + $files.last().height() + paddingTop;
        $(".dropbox", context.widget)
            .css("position", "absolute")
            .css("left", left)
            .css("top", top)
            .width(width)
            .height(height)
            .show();
    },
    hideDropBox = function (context) {
        $(".dropbox", context.widget).hide().html(dropBoxDefaultHtml);
    },
    attachHandlers = function (context) {

        $(context.widget).on("click", ".query-filter a[sortBy]", function (e) {
            e.preventDefault();
            $(this).closest('.query-filter').find('.filter-option').removeClass('selected');
            $(this).closest('.query-filter .filter-option').addClass('selected');
            context.sortBy = $(this).attr("sortBy");
            context.sortOrder = $(this).attr("sortOrder");
            refresh(defaultViewUrl(context), context);
        }).on("click", ".view-type .list-view", function (e) {
            e.preventDefault();
            $(this).closest('.view-type').find('.filter-option').removeClass('selected');
            $(this).closest('.view-type .filter-option').addClass('selected');
            context.view = context.listViewName;
            refresh(context.listViewUrl, context);
        }).on("click", ".view-type .explorer-view", function (e) {
            e.preventDefault();
            $(this).closest('.view-type').find('.filter-option').removeClass('selected');
            $(this).closest('.view-type .filter-option').addClass('selected');
            context.view = context.explorerViewName;
            refresh(context.explorerViewUrl, context);
        }).on("click", ".open-folder[path]", function (e) {
            e.preventDefault();
            context.page = 0;
            window.location.hash = $(this).attr('path');
        }).on("click", ".file-list-footer .pager a", function (e) {
            e.preventDefault();
            var m = this.href.match(/page=(\d+)/);
            if (m && m.length > 0) {
                context.page = m[1];
            }
            refresh(defaultViewUrl(context), context);
        }).on("click", ".new-folder", function (e) {
            e.preventDefault();
            createFolder(context);
        }).on("click", ".upload-document", function (e) {
            e.preventDefault();
            var dir = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
            $.glowModal(context.uploadDocumentUrl.updateQueryString('libraryId', context.libraryId).updateQueryString('folderPath', escape(dir)), {
                width: context.uploadDocumentWidth,
                height: context.uploadDocumentHeight,
                onClose: function (response) {
                    if (response && response.valid) {
                        refresh(defaultViewUrl(context), context);
                    }
                }
            });
        });

        if (context.dragAndDropEnabled && fileSystemAPIsSupported) {
            // HTML 5 Drag & Drop
            $(context.widget).on("dragover", ".file-list", function (e) {
                showDropBox(context);
            });

            var dropBoxElement = $(".dropbox", context.widget)[0];
            dropBoxDefaultHtml = dropBoxElement.innerHTML;
            dropBoxElement.ondragleave = function () {
                hideDropBox(context);
                return false;
            };
            dropBoxElement.ondragover = function (e) {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = 'copy';
            };
            dropBoxElement.ondragenter = function (e) {
                e.preventDefault();
                e.stopPropagation();
            };
            dropBoxElement.ondrop = function (e) {
                e.preventDefault();
                e.stopPropagation();
                handleDroppedFiles(e.dataTransfer.files, context);
            };
        }

        $(window).bind('hashchange', function () {
            context.folderPath = window.location.hash.split('#')[1];
            refresh(defaultViewUrl(context), context);
        });

        // Handling API functions
        $(context.widget).bind(api.update, function (e, data) {
            postUpdates(data, context);
        }).bind(api.rename, function (e, data) {
            renameFolder(data.contentId, data.folderPath, context);
        }).bind(api.refresh, function () {
            refresh(defaultViewUrl(context), context);
        }).bind(api.versions, function (e, data) {
            openFileVersions(data.contentId, context);
        }).bind(api.permissions, function (e, data) {
            openPermissions(data.contentId, context);
        });
    },
    handleDroppedFiles = function (files, context) {
        var filesProcessedCount = 0,
            $files = $('<ul class="dropped-files"></ul>');
        for (var i = 0, filesCount = files.length; i < filesCount; i++) {
            var f = files[i],
                $f = $(['<li><strong>', document.createTextNode(f.name).textContent, '</strong>', /*' - ', f.size, ' bytes',*/
                            '<div class="progress-bar" style="display:none;"><div class="percent">0%</div></div>',
                         '</li>'].join(''));
            // Show progress if size is more than 10 MB
            if (f.size > 10 * 1024 * 1024) {
                $(".progress-bar", $f).show();
            }
            $files.append($f);
            $p = $(".percent", $f);
            var reader = new FileReader();
            reader.onloadstart = function (e) {
                $(".progress-bar", $f).addClass('in-progress');
            };
            reader.onprogress = function (e) {
                if (e.lengthComputable) {
                    var loaded = Math.round((e.loaded / e.total) * 100);
                    if (loaded < 100) {
                        $p.css("width", loaded + '%').text(loaded + '%');
                    }
                }
            };
            reader.onload = (function (file, $holder) {
                return function (e) {
                    $(".progress-bar", $holder).removeClass('in-progress');

                    var url = context.uploadFileDirectUrl.updateQueryString('libraryId', context.libraryId);
                    var dir = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
                    url = url.updateQueryString('folderPath', escape(dir));

                    $.telligent.evolution.put({
                        url: url,
                        data: {
                            fileName: escape(file.name),
                            fileDataUrl: e.target.result
                        },
                        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                        success: function (responseHtml) {
                            $holder.addClass("success");
                        },
                        error: function (xhr) {
                            $holder.addClass("error");
                        },
                        complete: function () {
                            $(".percent", $f).text("100%").css("width", '100%');
                            filesProcessedCount++;
                        }
                    });
                };
            })(f, $f);
            reader.readAsDataURL(f);
        }
        $(".dropbox", context.widget).html($files);
        var intervarId = setInterval(function () {
            if (filesProcessedCount == filesCount) {
                // loading completed
                clearInterval(intervarId);
                setTimeout(function () {
                    hideDropBox(context);
                    refresh(defaultViewUrl(context), context);
                }, 2000);
            }
        }, 500);
    },
    refresh = function (url, context) {
        $('.file-list', context.contentHolderId).html('<div class="loading"></div>');
        $('.file-list-footer', context.contentHolderId).remove();
        var data = {
            libraryId: context.libraryId,
            w_sortby: context.sortBy,
            w_sortOrder: context.sortOrder,
            page: context.page || 0
        };
        data.w_FolderPath = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
        $.telligent.evolution.get({
            url: url.removeQueryStringKey('libraryId'),
            timeout: context.callTimeOut,
            data: data,
            success: function (responseHtml) {
                $(context.contentHolderId).html(responseHtml);
                $(global).trigger(documentLibraryEvent.updated, {
                    libraryRootFolder: context.libraryRootFolder,
                    folderPath: data.w_FolderPath
                });
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            },
            complete: function () {
                initContextMenu(context.contentHolderId, context.fileMenuItems, context.folderMenuItems);
            }
        });
    },
    defaultViewUrl = function (context) {
        return (context.view === context.listViewName) ? context.listViewUrl : context.explorerViewUrl;
    },
    postUpdates = function (data, context) {
        $.telligent.evolution.post({
            url: context.updateUrl,
            data: data,
            success: function () {
                refresh(defaultViewUrl(context), context);
            },
            error: function (xhr) {
                var errorMsg;
                try {
                    var errorHolder = eval('(' + xhr.responseText + ')');
                    errorMsg = errorHolder.Errors[0];
                }
                catch (ex) {
                    errorMsg = xhr.responseText;
                }
                window.parent.$.telligent.evolution.notifications.show(errorMsg, { type: 'error' });
            }
        });
    },
    createFolder = function (context) {
        var createFolderModalUrl = context.createFolderUrl.updateQueryString('libraryId', context.libraryId);
        var dir = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
        createFolderModalUrl = createFolderModalUrl.updateQueryString('folderPath', escape(dir));
        $.glowModal(createFolderModalUrl, {
            width: context.createFolderWidth,
            height: context.createFolderHeight,
            onClose: function (response) {
                if (response && response.valid) {
                    refresh(defaultViewUrl(context), context);
                }
            }
        });
    },
    renameFolder = function (contentId, folderPath, context) {
        var dir = (folderPath && folderPath.length > 0) ? folderPath : context.libraryRootFolder;
        var renameFolderModalUrl = context.createFolderUrl.updateQueryString('contentId', contentId).updateQueryString('folderPath', escape(dir));
        $.glowModal(renameFolderModalUrl, {
            width: context.createFolderWidth,
            height: context.createFolderHeight,
            onClose: function (responseObj) {
                if (responseObj && responseObj.valid) {
                    refresh(defaultViewUrl(context), context);
                }
            }
        });
    },
    openFileVersions = function (contentId, context) {
        $.glowModal(context.versionsModalUrl.updateQueryString('contentId', contentId), {
            width: context.versionsModalWidth,
            height: context.versionsModalHeight,
            onClose: function (response) {
                if (response == "true") {
                    refresh(defaultViewUrl(context), context);
                }
            }
        });
    },
    openPermissions = function (contentId, context) {
        $.glowModal(context.permissionsModalUrl.updateQueryString('contentId', contentId), {
            width: context.permissionsModalWidth,
            height: context.permissionsModalHeight,
            onClose: function () {
            }
        });
    };

    $.telligent.sharepoint.widgets.documentLibrary = {
        register: function (context) {
            var options = $.extend({}, defaultOptions, context);
            init(options);
            attachHandlers(options);
        },
        api: function (widgetId, command, data) {
            $(widgetId).trigger(command, data);
        }
    };

    //----------- Context Menu Utility -------------//
    // Right Click currentContext menu
    var initContextMenu = function (holderId, filemenu, foldermenu) {
        var $holder = $(holderId);
        $(function () {
            if (filemenu) {
                if (filemenu.length > 0) {
                    fillContextMenu($holder.find(".file-preview").closest(".file-info"), filemenu);
                }
                    // There are no items to display
                else {
                    disableContextMenu($holder.find(".file-preview").closest(".file-info"));
                }
            }
            if (foldermenu) {
                if (foldermenu.length > 0) {
                    fillContextMenu($holder.find(".folder-preview").closest(".file-info"), foldermenu);
                }
                    // There are no items to display
                else {
                    disableContextMenu($holder.find(".folder-preview").closest(".file-info"));
                }
            }
        });

        function fillContextMenu(holder, menu) {
            holder.each(function () {
                var target = $(this).find(".file-preview");
                $(this).contextMenu(extendedContextMenu(menu, target), {
                    beforeShow: function () { $(document).click(); },
                    shadow: false,
                    className: "file-menu",
                    itemClassName: "file-menu-item",
                    itemHoverClassName: "hover",
                    separatorClassName: "file-menu-item-separator",
                    rightClick: true
                });
            });
        }

        function extendedContextMenu(menu, target) {
            var items = [];
            if (menu && menu.length > 0) {
                for (var i = 0; i < menu.length; i++) {
                    if (typeof (menu[i].isVisible) == "function") {
                        if (menu[i].isVisible(target))
                            items.push(menu[i].item);
                    }
                    else items.push(menu[i]);
                }
            }
            return items;
        }

        function disableContextMenu(holder) {
            holder.each(function () {
                $(this).bind("contextmenu", function () {
                    return false;
                });
            });
        }
    };
})(jQuery, window);
