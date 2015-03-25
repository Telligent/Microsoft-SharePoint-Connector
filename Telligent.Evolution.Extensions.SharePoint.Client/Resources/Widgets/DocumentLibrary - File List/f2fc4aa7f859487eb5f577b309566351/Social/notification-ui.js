// $.telligent.sharepoint.widgets.documentNotification
(function ($, global) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var defaultOptions = {
        wrapper: document,
        userNamesEmailsHolder: null,
        getMessage: null,
        sendNotificationButton: ".internal-link.send-button",
        sendNotificationUrl: null,
        findUsersOrEmailsUrl: null,
        noUserOrEmailMatchesText: "No users or emails found",
        addUserNameEmailTimeout: 500,
        subject: "FW: Document Link",
        body: "",
        userIds: "",
        userEmails: ""
    },
    $sendNotification,
    $userNameOrEmail,
    init = function (context) {
        $sendNotification = $(context.sendNotificationButton, context.wrapper).addClass('disabled');
        $userNameOrEmail = $(context.userNamesEmailsHolder, context.wrapper).glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: false,
            maxValues: 20,
            onGetLookUps: function (tb, searchText) {
                if (context.__addUserNameEmailTimeoutId) window.clearTimeout(context.__addUserNameEmailTimeoutId);
                if (searchText && searchText.length >= 2) {
                    tb.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', '<div style="text-align: center;"><img src="' + $.telligent.evolution.site.getBaseUrl() + 'utility/spinner.gif" alt="" /></div>', '<div style="text-align: center;"><img src="' + $.telligent.evolution.site.getBaseUrl() + 'utility/spinner.gif" alt="" /></div>', false)]);
                    context.__addUserNameEmailTimeoutId = window.setTimeout(function () {
                        $.telligent.evolution.get({
                            url: context.findUsersOrEmailsUrl,
                            data: { w_SearchText: searchText },
                            success: function (response) {
                                if (response && response.matches.length > 0) {
                                    var suggestions = [];
                                    for (var i = 0, len = response.matches.length; i < len; i++) {
                                        var item = response.matches[i];
                                        if (item && item.userId) {
                                            suggestions.push(tb.glowLookUpTextBox('createLookUp', 'user:' + item.userId, item.title, item.title, true));
                                        }
                                        else if (item && item.email) {
                                            suggestions.push(tb.glowLookUpTextBox('createLookUp', 'email:' + item.email, item.email, item.email, true));
                                        }
                                    }
                                    tb.glowLookUpTextBox('updateSuggestions', suggestions);
                                }
                                else
                                    tb.glowLookUpTextBox('updateSuggestions', [tb.glowLookUpTextBox('createLookUp', '', context.noUserOrEmailMatchesText, context.noUserOrEmailMatchesText, false)]);
                            }
                        });
                    }, context.addUserNameEmailTimeout);
                }
            }
        })
    },
    attachHandlers = function (context) {
        $sendNotification.click(function (e) {
            e.preventDefault();
            if ($(this).hasClass('disabled')) return;

            var sendNotificationText = $sendNotification.addClass('disabled').text();
            $sendNotification.text($sendNotification.data('sending') || sendNotificationText);
            $.telligent.evolution.post({
                url: context.sendNotificationUrl,
                data: getFormData(context),
                success: function (response) {
                    if (response && response.valid) {
                        window.parent.$.glowModal.opener(window).$.glowModal.close(true);
                    }
                },
                complete: function () {
                    $sendNotification.removeClass('disabled').text(sendNotificationText);
                }
            });
        });
        $userNameOrEmail.bind("glowLookUpTextBoxChange", function () {
            var count = $userNameOrEmail.glowLookUpTextBox('count');
            if (count > 0) $sendNotification.removeClass('disabled');
            else $sendNotification.addClass('disabled');
        });
    },
    getFormData = function (context) {
        var data = {
            subject: context.subject,
            body: context.getMessage()
        };
        var userIds = [],
            userEmails = [],
            count = $userNameOrEmail.glowLookUpTextBox('count');
        for (var i = 0; i < count; i++) {
            var result = $userNameOrEmail.glowLookUpTextBox('getByIndex', i).Value;
            var userMatch = /^user:([0-9]+)$/g.exec(result);
            if (userMatch && userMatch.length == 2) {
                userIds.push(userMatch[1]);
            }
            else if (/^email:([\w.-]+)@([\w-]+)((\.(\w){2,3})+)$/g.test(result)) {
                userEmails.push(result.substr("email:".length, result.length));
            }
        }
        data.userIds = userIds.join(',');
        data.userEmails = userEmails.join(',');
        return data;
    };

    $.telligent.sharepoint.widgets.documentNotification = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery, window)
