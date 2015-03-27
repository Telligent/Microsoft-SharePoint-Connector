(function ($) {
    if (typeof $.telligent === 'undefined')
        $.telligent = {};

    if (typeof $.telligent.evolution === 'undefined')
        $.telligent.evolution = {};

    if (typeof $.telligent.evolution.extensions === 'undefined')
        $.telligent.evolution.extensions = {};

    var initialize = function (context) {
        var url = context.WebItemControl.val();
        var listId = context.LookUpTextBox.val();

        var isEmpty = (url == '' || listId == '');
        if (isEmpty) {
            initializeControl(context);
            return;
        }

        loadingStarted(context);
        $.telligent.evolution.get({
            url: '/api.ashx/v2/sharepoint/lists/{listId}.json',
            data: {
                url: url,
                listId: listId
            },
            success: function (response) {
                var list = response.List.Item;
                initializeControl(context, list.Title);
            },
            complete: function () {
                loadingCompleted(context);
            }
        });
    },

    initializeControl = function (context, startValue) {
        context.LookUpTextBox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPLists(context, tb, searchText)
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });
    },

    lookingForSPLists = function (context, textbox, searchText) {
        var hasUrl = context.WebItemControl.val() != '';
        var hasSearchText = searchText && searchText.length >= 1;
        if (hasUrl && hasSearchText) {
            loadSPLists(context, textbox, searchText);
        }
    },

    loadSPLists = function (context, textbox, searchText) {
        var url = context.WebItemControl.val();
        listType = context.listType || "";
        excludeType = context.excludeType || "";
        spinner = context.Spinner;
        loadingStarted(context);
        $.telligent.evolution.get({
            url: '/api.ashx/v2/sharepoint/lists.json',
            data: {
                url: url,
                listType: listType,
                excludeType: excludeType,
                ListNameFilter: searchText
            },
            success: function (response) {
                updateControl(textbox, response.Lists.Items, spinner);
            },
            complete: function () {
                loadingCompleted(context);
            }
        });
    },

    updateControl = function (textbox, listCollection, spinner) {
        textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
        textbox.glowLookUpTextBox('updateSuggestions',
            $.map(listCollection, function (list, i) {
                var markup = list.Title;
                return textbox.glowLookUpTextBox('createLookUp', list.Id, list.Title, markup, true);
            })
        );
    },

    // Show visual effects while loading
    loadingStarted = function (context) {
        context.LookUpTextBox.hide();
        context.LookUpTextBox.before(context.Loader);
    },

    // Show visual effects when loading completed
    loadingCompleted = function (context) {
        context.LookUpTextBox.show();
        context.LookUpTextBox.closest(".field-item-input").find(".loading").remove();
    },

    attachHandlers = function (context) {
        context.WebItemControl.bind('glowLookUpTextBoxChange', function () {
            var disabled = context.WebItemControl.val() == '';
            context.LookUpTextBox.glowLookUpTextBox('removeByIndex', 0);
            context.LookUpTextBox.glowLookUpTextBox('disabled', disabled)
        });
    };

    $.telligent.evolution.extensions.lookupSharePointList = {
        register: function (context) {
            initialize(context);
            attachHandlers(context);
        }
    };
})(jQuery);