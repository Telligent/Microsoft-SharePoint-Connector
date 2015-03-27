(function ($) {
	window.console = window.console || { log: function () { } };
	$.telligent = $.telligent || {};
	$.telligent.sharepoint = $.telligent.sharepoint || {};
	$.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

	var modalWidthDefaultValue = 750;
	var modalHeightDefaultValue = 500;
	var init = function (context) {
		context.modalWidth = context.modalWidth || modalWidthDefaultValue;
		context.modalHeight = context.modalHeight || modalHeightDefaultValue;
	},
	attachHandlers = function (context) {
		$(".versioning", context.contentHolderId).click(function (e) {
			e.preventDefault();
			$.glowModal(context.versioningUrl, {
				width: context.modalWidth,
				height: context.modalHeight,
				onClose: function (response) {
					if (response && response.valid) {
						window.location.reload();
					}
				}
			});
		});
	};
	$.telligent.sharepoint.widgets.syncStatus = {
		register: function (context) {
			init(context);
			attachHandlers(context);
		}
	};
})(jQuery);
