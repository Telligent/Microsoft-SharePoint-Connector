jQuery(function ($) {
	$.telligent = $.telligent || {};
	$.telligent.sharepoint = $.telligent.sharepoint || {};
	$.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};
	$.telligent.sharepoint.widgets.documentLibrary = $.telligent.sharepoint.widgets.documentLibrary || {};

	var attachHandlers = function (context) {
		$(context.wrapper).bind('evolutionModerateLinkClicked', function (event, target) {
			event.preventDefault();
			if ($(target).hasClass('delete-post')) {
				deleteDocument(context);
			}
		});
	},
	deleteDocument = function (context) {
		if (confirm(context.deleteVerificationText)) {
			$.telligent.evolution.del({
				url: context.deleteDocumentUrl,
				data: {
					documentId: context.documentId
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
$.telligent.sharepoint.widgets.documentLibrary.file = {
		register: function (context) {
			attachHandlers(context);
		}
	};
});