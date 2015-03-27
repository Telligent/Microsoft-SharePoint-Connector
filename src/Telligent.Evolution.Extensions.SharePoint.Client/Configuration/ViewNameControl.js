(function ($) {
    if (typeof $.telligent === 'undefined')
        $.telligent = {};

    if (typeof $.telligent.evolution === 'undefined')
        $.telligent.evolution = {};

    if (typeof $.telligent.evolution.extensions === 'undefined')
        $.telligent.evolution.extensions = {};

    var initialize = function (context) {
        var url = context.WebItemControl.val();
        var listId = context.ListTextBox.val();
        var viewId = context.ViewTextBox.val();

        var isEmpty = (url == '' || listId == '' || viewId == '');
        if (isEmpty) {
            context.ViewTextBox.hide();
            initializeControl(context);
            return;
        }

        loadingStarted(context);
        $.telligent.evolution.get({
            url: '/api.ashx/v2/sharepoint/lists/{listId}/views/{viewId}.json',
            data: {
                url: url,
                listId: listId,
                viewId: viewId
            },
            success: function (response) {
                var view = response.View.Item;
                initializeControl(context, view.Title);
            },
            complete: function () {
                loadingCompleted(context);
            }
        });
    },

    initializeControl = function (context, startValue) {
        context.ViewTextBox.glowLookUpTextBox({
            delimiter: ',',
            allowDuplicates: true,
            maxValues: 1,
            onGetLookUps: function (tb, searchText) {
                lookingForSPViews(context, tb, searchText)
            },
            emptyHtml: '',
            selectedLookUpsHtml: (!startValue || startValue == '') ? [] : [startValue],
            deleteImageUrl: ''
        });
    },

    lookingForSPViews = function (context, textbox, searchText) {
        var hasUrl = context.WebItemControl.val() != '';
        var hasListId = context.ListTextBox.val() != '';
        var hasSearchText = searchText && searchText.length >= 1;
        if (hasUrl && hasListId && hasSearchText) {
            loadSPViews(context, textbox, searchText);
        }
    },

    loadSPViews = function (context, textbox, searchText) {
        var url = context.WebItemControl.val();
        var listId = context.ListTextBox.val();
        spinner = context.Spinner;
        loadingStarted(context);
        $.telligent.evolution.get({
            url: '/api.ashx/v2/sharepoint/lists/{listId}/views.json',
            data: {
                url: url,
                listId: listId,
                ViewNameFilter: searchText
            },
            success: function (response) {
                updateControl(textbox, response.Views.Items, spinner);
            },
            complete: function () {
                loadingCompleted(context);
            }
        });
    },

    updateControl = function (textbox, viewCollection, spinner) {
        textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
        textbox.glowLookUpTextBox('updateSuggestions',
            $.map(viewCollection, function (view, i) {
                var markup = view.Title;
                return textbox.glowLookUpTextBox('createLookUp', view.Id, view.Title, markup, true);
            })
        );
    },

    // Show visual effects while loading
    loadingStarted = function (context) {
        context.ViewTextBox.hide();
        context.ViewTextBox.before(context.Loader);
    },

    // Show visual effects when loading completed
    loadingCompleted = function (context) {
        context.ViewTextBox.show();
        context.ViewTextBox.closest(".field-item-input").find(".loading").remove();
    },

    attachHandlers = function (context) {
        context.ListTextBox.bind('glowLookUpTextBoxChange', function () {
            var disabled = context.ListTextBox.val() == '';
            context.ViewTextBox.glowLookUpTextBox('removeByIndex', 0);
            context.ViewTextBox.glowLookUpTextBox('disabled', disabled)
        });
        context.WebItemControl.bind('glowLookUpTextBoxChange', function () {
            var disabled = context.WebItemControl.val() == '' || context.ListTextBox.val() == '';
            context.ViewTextBox.glowLookUpTextBox('removeByIndex', 0);
            context.ViewTextBox.glowLookUpTextBox('disabled', disabled)
        });
    };

    $.telligent.evolution.extensions.lookupSharePointViewName = {
        register: function (context) {
            initialize(context);
            attachHandlers(context);
        }
    };
})(jQuery);