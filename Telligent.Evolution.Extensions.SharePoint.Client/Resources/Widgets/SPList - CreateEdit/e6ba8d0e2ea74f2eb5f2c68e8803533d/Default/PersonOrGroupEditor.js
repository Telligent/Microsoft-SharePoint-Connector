(function ($) {
    window.console = window.console || { log: function () { } };
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var spinner = '',
    initPersonOrGroupTextbox = function (context) {
        $(context.personOrGroupTextbox).glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: false,
            maxValues: context.allowMultipleValues ? 10 : 1,
            onGetLookUps: function (tb, searchText) {
                searchUserOrGroup(context, tb, searchText, false);
            },
            emptyHtml: '',
            selectedLookUpsHtml: [],
            deleteImageUrl: ''
        });
    },
    personOrGroupValues = function (context) {
        var values = [];
        for (var i = 0, len = $(context.personOrGroupTextbox).glowLookUpTextBox('count'); i < len; i++) {
            var personOrGroup = $(context.personOrGroupTextbox).glowLookUpTextBox('getByIndex', i);
            values.push(personOrGroup.Value.Name);
        }

        $(context.selectedLookUps).each(function () {
            values.push($(this).attr('personOrGroup'));
        });

        return values.join(";");
    },
    searchUserOrGroup = function (context, textbox, searchText, onlyGroups) {
        if (searchText && searchText.length > 0) {
            searchText = searchText.trim();
            textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
            $.telligent.evolution.get({
                url: (onlyGroups) ? '/api.ashx/v2/sharepoint/groups.json' : '/api.ashx/v2/sharepoint/usersandgroups.json',
                data: {
                    url: context.webUrl,
                    search: searchText
                },
                success: function (response) {
                    var lookUpSuggestions = [];
                    for (var i = 0, len = response.List.length; i < len; i++) {
                        var personOrGroup = response.List[i];
                        if (!onlyGroups || (onlyGroups && personOrGroup.IsGroup)) {
                            if (personOrGroup.Email != null && personOrGroup.Email.length > 0) {
                                var displayName = (personOrGroup.DisplayName != personOrGroup.Title) ? [personOrGroup.DisplayName, " (", personOrGroup.Title, ")"].join('') : personOrGroup.DisplayName;
                                var markup = [displayName, "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px; color: #777;' title='", personOrGroup.Title, "'>", personOrGroup.Email, "</div>"].join('');
                                lookUpSuggestions.push(textbox.glowLookUpTextBox('createLookUp', personOrGroup, displayName, markup, true));
                            }
                        }
                    }
                    textbox.glowLookUpTextBox('updateSuggestions', lookUpSuggestions);
                }
            });
        }
    };

    $.telligent.sharepoint.widgets.personOrGroupEditor = {
        register: function (context) {
            initPersonOrGroupTextbox(context);

            $("a", context.selectedLookUps).click(function (e) {
                e.preventDefault();

                $(this).parent().parent().remove();

                if ($(context.selectedLookUps).length == 0) {
                    $(context.personOrGroupPreSelected).hide();
                    $(context.personOrGroupContainer).show();
                }

            });
        },
        getValues: function (context) {
            return personOrGroupValues(context);
        }
    };
})(jQuery);
