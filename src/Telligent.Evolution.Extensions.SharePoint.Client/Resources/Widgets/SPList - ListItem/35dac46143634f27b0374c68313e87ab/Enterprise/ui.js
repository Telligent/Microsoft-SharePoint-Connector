jQuery(function ($) {
	$.telligent = $.telligent || {};
	$.telligent.sharepoint = $.telligent.sharepoint || {};
	$.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
	$.telligent.sharepoint.widgets.listItem = $.telligent.sharepoint.widgets.listItem || {};

	var attachHandlers = function (context) {
		$(context.wrapper).bind('evolutionModerateLinkClicked', function (event, target) {
			event.preventDefault();
			if ($(target).hasClass('delete-post')) {
				deleteListItem(context);
			}
		});
	},
	deleteListItem = function (context) {
		if (confirm(context.deleteVerificationText)) {
			$.telligent.evolution.del({
				url: context.deleteListItemUrl,
				data: {
					itemIds: context.listItemId
				},
				dataType: 'json',
				success: function (response) {
					if (response && response.valid) {
						window.parent.$.telligent.evolution.notifications.show(response.statusMsg, { type: 'warning' });
						setTimeout(function () {
							window.location = response.UrlRedirect;
						}, 1000);
					}
				},
				error: function (jqXHR, textStatus, errorThrown) {
					console.log(textStatus);
					window.parent.$.telligent.evolution.notifications.show(textStatus, { type: 'error' });
				}
			});
		}
	};
	$.telligent.sharepoint.widgets.listItem.properties = {
		register: function (context) {
			attachHandlers(context);
		}
	};
});