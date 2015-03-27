(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: null,

        contentHolderId: '',
        libraryId: null,
        libraryRootFolder: null,

        viewType: 'ListView',
        listViewName: 'ListView',
        listViewUrl: null,
        explorerViewName: 'ExplorerView',
        explorerViewUrl: null,

        sortBy: 'FileLeafRef',
        sortOrder: 'Ascending',
        page: 0,

        checkInUrl: null,
        checkInModalUrl: null,
        deleteDocumentUrl: null,

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

        sendNotificationModalWidth: 780,
        sendNotificationModalHeight: 300,
        sendNotificationModalUrl: null,
        notificationSendSuccessfullyText: "",

        uploadFileDirectUrl: null,
        hasEditPermissions: false,
        noPermissionsError: "You do not have permissions to perform the requested action",

        deleteFileConf: "You are about to delete file, would you like to continue?",
        deleteFolderConf: "You are about to delete folder, would you like to continue?",

        callTimeOut: 60000
    },
    api = {
        rename: 'rename',
        refresh: 'refresh',
        versions: 'versions',
        permissions: 'permissions'
    },
    apiEvent = {
        added: 'documentLibraryAdded',
        removed: 'documentLibraryRemoved',
        updated: 'documentLibraryUpdated'
    },
    init = function (context) {
        if (context.folderPath && context.folderPath.length > 0) {
            window.location.hash = context.folderPath;
        }
        else {
            context.folderPath = window.location.hash.split('#')[1];
        }
        refresh(context);
        $(global).bind(apiEvent.removed, function (e, documentLibraryRemovedEventArgs) {
            if (documentLibraryRemovedEventArgs.count == 0) {
                context.widget.hide();
            }
        });
    },
    setupHeaderLinks = function (context) {
        context.viewType = $('.view select', context.wrapper).val();
        $('.view select', context.wrapper).on('change', function (e) {
            context.viewType = $(e.target).val();
            refresh(context);
        });

        context.sortBy = $('.sort .by select', context.wrapper).val();
        $('.sort .by select', context.wrapper).on('change', function () {
            context.sortBy = $('.sort .by select', context.wrapper).val();
            refresh(context);
        });

        context.sortOrder = $('.sort .order select', context.wrapper).val();
        $('.sort .order select', context.wrapper).on('change', function () {
            context.sortOrder = $('.sort .order select', context.wrapper).val();
            refresh(context);
        });

        $.telligent.evolution.messaging.subscribe('newFolderSubscribe', function () {
            createFolder(context);
        });

        $.telligent.evolution.messaging.subscribe('newFileSubscribe', function () {
            var folderPath = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
            $.glowModal(context.uploadDocumentUrl + "&folderPath=" + window.escape(folderPath), {
                width: context.uploadDocumentWidth,
                height: context.uploadDocumentHeight,
                onClose: function (response) {
                    if (response && response.valid) {
                        $(global).trigger(apiEvent.updated, {
                            libraryRootFolder: context.libraryRootFolder,
                            folderPath: context.folderPath && context.folderPath.length > 0 ? context.folderPath : context.libraryRootFolder
                        });
                        refresh(context);
                    }
                }
            });
        });
    },
    setupContentLinks = function (context) {
        $(".pager a", context.contentHolderId).on("click", function (e) {
            e.preventDefault();
            var m = this.href.match(/page=(\d+)/);
            if (m && m.length > 0) {
                context.page = m[1];
            }
            refresh(context);
        });
    },
    attachHandlers = function (context) {
        var getUILinks = function (target) {
            var $uiLinks = $(target).closest('ul');
            return {
                chickIn: $uiLinks.find('a[data-type="checkIn"]'),
                checkOut: $uiLinks.find('a[data-type="checkOut"]'),
                discardCheckOut: $uiLinks.find('a[data-type="discardCheckOut"]')
            };
        };

        $.telligent.evolution.messaging.subscribe('editDocumentSubscribe', function (e) {
            var fileName = $(e.target).attr("href");
            if (fileName && fileName.length > 0) {
                if (typeof context.browserExtensions !== "undefined") {
                    var result = context.browserExtensions.editDocument(fileName);
                    if (result.hasErrors) {
                        $.telligent.evolution.notifications.show(result.error, { type: 'error' });
                    }
                    return false;
                }
            }
        });

        $.telligent.evolution.messaging.subscribe('checkInSubscribe', function (e) {
            var contentId = $(e.target).data("contentid");
            $.glowModal(context.checkInModalUrl + "&contentId=" + contentId, {
                onClose: function (res) {
                    if (res && !res.isCheckedOut) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.hide();
                        $uiLinks.discardCheckOut.hide();
                        $uiLinks.checkOut.show();
                        $(context.contentHolderId).find('.is-checked-out.' + contentId).hide();
                    }
                }
            });
        });

        $.telligent.evolution.messaging.subscribe('discardCheckOutSubscribe', function (e) {
            var contentId = $(e.target).data("contentid");
            $.telligent.evolution.post({
                url: context.checkInUrl,
                data: {
                    contentId: contentId,
                    method: "discardcheckout"
                },
                success: function (res) {
                    if (res) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.hide();
                        $uiLinks.discardCheckOut.hide();
                        $uiLinks.checkOut.show();
                        $(context.contentHolderId).find('.is-checked-out.' + contentId).hide();
                    }
                },
                error: showError
            });
        });

        $.telligent.evolution.messaging.subscribe('checkOutSubscribe', function (e) {
            var contentId = $(e.target).data("contentid");
            $.telligent.evolution.post({
                url: context.checkInUrl,
                data: {
                    contentId: contentId,
                    method: "checkout"
                },
                success: function (res) {
                    if (res) {
                        var $uiLinks = getUILinks(e.target);
                        $uiLinks.chickIn.show();
                        $uiLinks.discardCheckOut.show();
                        $uiLinks.checkOut.hide();
                        $(context.contentHolderId).find('.is-checked-out.' + contentId).show();
                    }
                },
                error: showError
            });
        });

        $.telligent.evolution.messaging.subscribe('versionHistorySubscribe', function (e) {
            openFileVersions($(e.target).data("contentid"), context);
        });

        $.telligent.evolution.messaging.subscribe('deleteFileSubscribe', function (e) {
            var contentId = $(e.target).data("contentid");
            if (global.confirm(context.deleteFileConf)) {
                $(context.contentHolderId).find('.content-item[data-contentid="' + contentId + '"]').addClass('processing');
                $.telligent.evolution.del({
                    url: context.deleteDocumentUrl,
                    data: { contentId: contentId },
                    success: function () {
                        refresh(context);
                        $(global).trigger(apiEvent.updated, {
                            libraryRootFolder: context.libraryRootFolder,
                            folderPath: context.folderPath && context.folderPath.length > 0 ? context.folderPath : context.libraryRootFolder
                        });
                    },
                    error: showError
                });
            }
        });
        $.telligent.evolution.messaging.subscribe('sendNotificationSubscribe', function (e) {
            sendNotification($(e.target).data("contentid"), context);
        });
        $.telligent.evolution.messaging.subscribe('managePermissionsSubscribe', function (e) {
            openPermissions($(e.target).data("contentid"), context);
        });
        $.telligent.evolution.messaging.subscribe('renameFolderSubscribe', function (e) {
            renameFolder($(e.target).data("contentid"), $(e.target).data("path"), context);
        });
        $.telligent.evolution.messaging.subscribe('deleteFolderSubscribe', function (e) {
            var contentId = $(e.target).data("contentid");
            if (global.confirm(context.deleteFolderConf)) {
                $(context.contentHolderId).find('.content-item[data-contentid="' + contentId + '"]').addClass('processing');
                $.telligent.evolution.del({
                    url: context.deleteDocumentUrl,
                    data: { contentId: contentId },
                    success: function () {
                        refresh(context);
                        $(global).trigger(apiEvent.updated, {
                            libraryRootFolder: context.libraryRootFolder,
                            folderPath: context.folderPath && context.folderPath.length > 0 ? context.folderPath : context.libraryRootFolder
                        });
                    },
                    error: showError
                });
            }
        });

        $(global).bind('hashchange', function () {
            context.folderPath = window.location.hash.split('#')[1];
            refresh(context);
        });

        // Handling API functions
        $(context.wrapper).bind(api.rename, function (e, data) {
            renameFolder(data.contentId, data.folderPath, context);
        }).bind(api.refresh, function () {
            refresh(context);
        }).bind(api.versions, function (e, data) {
            openFileVersions(data.contentId, context);
        }).bind(api.permissions, function (e, data) {
            openPermissions(data.contentId, context);
        });
    },
    setupClickableItems = function (context) {
        $(context.wrapper).on('click', '.content-item', function (e) {
            if (context.viewType === context.explorerViewName) {
                var url = $(this).data('url');
                if (url && url.length > 0) {
                    window.location = url;
                }
            }
        });
    },
    setupDragAndDrop = function (context) {
        var fileSystemApiSupported = global.File && global.FileReader && global.FileList && global.Blob;
        if (!fileSystemApiSupported) return;

        context.dropBoxBorders = $.extend({}, {
            left: 0,
            right: 0,
            top: 0,
            bottom: 0
        }, context.dropBoxBorders);

        var dropBoxDefaultHtml,
        showDropBox = function () {
            $updateDropBox().show();
            $(window).on("resize", function () {
                if ($(".dropbox", context.widget).is(':visible')) {
                    $updateDropBox();
                }
            });
        },
        getDropBoxPosition = function () {
            var dropBoxPosition = {
                left: 0,
                top: 0,
                width: 0,
                height: 0
            },
            $content = $('.content-list', context.contentHolderId),
            $norecords = $('.message.norecords', context.contentHolderId),
            $position;
            if ($norecords.length === 0) {
                $position = $content.position();
                dropBoxPosition.left = $position.left;
                dropBoxPosition.top = $position.top;
                dropBoxPosition.width = $content.width();
                dropBoxPosition.height = $content.height();
            }
            else {
                if ($content.length === 0) {
                    $position = $norecords.position();
                    dropBoxPosition.left = $position.left;
                    dropBoxPosition.top = $position.top;
                    dropBoxPosition.width = $norecords.outerWidth(true);
                    dropBoxPosition.height = $norecords.outerHeight(true);
                }
                else {
                    $position = $content.position();
                    dropBoxPosition.left = $position.left;
                    dropBoxPosition.top = $position.top;
                    dropBoxPosition.width = $content.width();
                    $norecords.bottom = $norecords.position().top + $norecords.outerHeight(true);
                    dropBoxPosition.height = $norecords.bottom - $position.top;
                }
            }
            return dropBoxPosition;
        },
        $updateDropBox = function () {
            var position = getDropBoxPosition();
            var height = position.height + context.dropBoxBorders.top + context.dropBoxBorders.bottom;
            return $(".dropbox", context.widget)
                    .css("position", "absolute")
                    .css("left", position.left - context.dropBoxBorders.left + "px")
                    .css("top", position.top - context.dropBoxBorders.top + "px")
                    .width(position.width + context.dropBoxBorders.left + context.dropBoxBorders.right)
                    .height(height > 200 ? height : 200);
        },
        hideDropBox = function () {
            $(".dropbox", context.widget).hide().html(dropBoxDefaultHtml);
            $(window).off("resize");
        },
        attachHandlers = function () {
            // HTML 5 Drag & Drop

            $(document).on("dragover", function (e) {
                showDropBox(context);
            }).on("dragleave", function (e) {
                var pageX = e.originalEvent.pageX;
                if (pageX <= 0 || pageX >= $(this).width()) {
                    hideDropBox(context);
                }
            });
            
            var dropBoxElement = $(".dropbox", context.widget)[0];
            dropBoxDefaultHtml = dropBoxElement.innerHTML;

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
                if (context.hasEditPermissions) {
                    handleDroppedFiles(e.dataTransfer.files, context);
                }
                else {
                    global.setTimeout(function () {
                        hideDropBox(context);
                    }, 500);
                }
            };
        },
        handleDroppedFiles = function (files) {
            var filesProcessedCount = 0,
                $files = $('<ul class="dropped-files"></ul>');
            for (var i = 0, filesCount = files.length; i < filesCount; i++) {
                var f = files[i],
                    $f = $(['<li class="file uploading"><span class="file-name">', document.createTextNode(f.name).textContent, '</span>', /*' - ', f.size, ' bytes',*/
                                '<div class="progress-bar" style="display: none;"><div class="percent">0%</div></div>',
                                '</li>'].join(''));
                // Show progress if size is more than 10 MB
                if (f.size > 10 * 1024 * 1024) {
                    $(".progress-bar", $f).show();
                }
                $files.append($f);
                $p = $(".percent", $f);
                var reader = new FileReader();
                reader.onloadstart = function () {
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
                        $holder.addClass("uploading");

                        $.telligent.evolution.put({
                            url: context.uploadFileDirectUrl,
                            data: {
                                fileName: window.escape(file.name),
                                folderPath: (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder,
                                fileDataUrl: e.target.result
                            },
                            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                            success: function () {
                                $holder.addClass("success");
                                setTimeout(function () { $holder.remove(); }, 1000);
                            },
                            error: function (xhr, desc, ex) {
                                $holder.addClass("error");
                                if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
                                    $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
                                }
                                else {
                                    $.telligent.evolution.notifications.show(desc, { type: 'error' });
                                }
                            },
                            complete: function () {
                                $holder.removeClass("uploading").addClass("completed");
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
                        refresh(context);
                    }, 2500);
                }
            }, 500);
        };
        attachHandlers();
    },
    refresh = function (context) {
        $(context.contentHolderId).html('<div class="loading"></div>');
        $.telligent.evolution.get({
            url: context.viewType === context.listViewName ? context.listViewUrl : context.explorerViewUrl,
            timeout: context.callTimeOut,
            data: {
                w_sortby: context.sortBy,
                w_sortOrder: context.sortOrder,
                w_FolderPath: (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder,
                page: context.page || 0
            },
            success: function (responseHtml) {
                $(context.contentHolderId).html(responseHtml);
                setupContentLinks(context);
            },
            error: showError
        });
    },
    showError = function (xhr, desc, ex) {
        if (xhr.responseJSON.Errors != null && xhr.responseJSON.Errors.length > 0) {
            $.telligent.evolution.notifications.show(xhr.responseJSON.Errors[0], { type: 'error' });
        }
        else {
            $.telligent.evolution.notifications.show(desc, { type: 'error' });
        }
    },
    createFolder = function (context) {
        var folderPath = (context.folderPath && context.folderPath.length > 0) ? context.folderPath : context.libraryRootFolder;
        $.glowModal(context.createFolderUrl + "&folderPath=" + folderPath, {
            width: context.createFolderWidth,
            height: context.createFolderHeight,
            onClose: function (response) {
                if (response && response.valid) {
                    refresh(context);
                    $(global).trigger(apiEvent.updated, {
                        libraryRootFolder: context.libraryRootFolder,
                        folderPath: context.folderPath && context.folderPath.length > 0 ? context.folderPath : context.libraryRootFolder
                    });
                }
            }
        });
    },
    renameFolder = function (contentId, folderPath, context) {
        var dir = (folderPath && folderPath.length > 0) ? folderPath : context.libraryRootFolder;
        $.glowModal(context.createFolderUrl + "&contentId=" + contentId + "&folderPath=" + dir, {
            width: context.createFolderWidth,
            height: context.createFolderHeight,
            onClose: function (responseObj) {
                if (responseObj && responseObj.valid) {
                    refresh(context);
                    $(global).trigger(apiEvent.updated, {
                        libraryRootFolder: context.libraryRootFolder,
                        folderPath: context.folderPath && context.folderPath.length > 0 ? context.folderPath : context.libraryRootFolder
                    });
                }
            }
        });
    },
    openFileVersions = function (contentId, context) {
        $.glowModal(context.versionsModalUrl + "&contentId=" + contentId, {
            width: context.versionsModalWidth,
            height: context.versionsModalHeight,
            onClose: function (response) {
                if (response == "true") {
                    refresh(context);
                }
            }
        });
    },
    openPermissions = function (contentId, context) {
        $.glowModal(context.permissionsModalUrl + '&contentId=' + contentId, {
            width: context.permissionsModalWidth,
            height: context.permissionsModalHeight,
            onClose: function () { }
        });
    },
    sendNotification = function (contentId, context) {
        $.glowModal(context.sendNotificationModalUrl + '&contentId=' + contentId, {
            width: context.sendNotificationModalWidth,
            height: context.sendNotificationModalHeight,
            onClose: function (send) {
                if (send) {
                    $.telligent.evolution.notifications.show(context.notificationSendSuccessfullyText, {
                        type: 'info'
                    });
                }
            }
        });
    };

    $.telligent.sharepoint.widgets.documentLibrary = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            setupHeaderLinks(context);
            attachHandlers(context);
            setupDragAndDrop(context);
            setupClickableItems(context);
            init(context);
        },
        api: function (widgetId, command, data) {
            $(widgetId).trigger(command, data);
        }
    };
})(jQuery, window);

// $.telligent.sharepoint.widgets.documentCheckIn
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: global.document,
        contentId: null,
        commentId: null,
        get_checkInType: function () {
            var $version = $(".checkin-version .version-list :radio:checked", this.wrapper);
            return $version.length > 0 ? $version.data('type') : null;
        },
        saveId: null,
        saveUrl: null
    },
    attachHandlers = function (context) {
        context.data = {
            contentId: context.contentId,
            method: "checkin"
        };

        var $saveButton = $(context.saveId, context.wrapper).bind("click", function (e) {
            e.preventDefault();

            $saveButton.addClass("disabled").parent().find('.processing').show();
            context.data.keepcout = $(context.keepCheckedOutId, context.wrapper).is(":checked");
            context.data.comment = $(context.commentId, context.wrapper).val();
            context.data.checkintype = context.get_checkInType();

            $.telligent.evolution.put({
                url: context.saveUrl,
                data: context.data,
                success: function (res) {
                    window.parent.$.glowModal.opener(window).$.glowModal.close({
                        isCheckedOut: context.data.keepcout
                    });
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
        });
    };

    $.telligent.sharepoint.widgets.documentCheckIn = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            attachHandlers(context);
        }
    };
})(jQuery, window);