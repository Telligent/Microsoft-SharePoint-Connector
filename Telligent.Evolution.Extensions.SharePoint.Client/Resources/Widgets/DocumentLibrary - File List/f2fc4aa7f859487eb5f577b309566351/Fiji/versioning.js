(function ($) {
	window.console = window.console || { log: function () { } };
	$.telligent = $.telligent || {};
	$.telligent.sharepoint = $.telligent.sharepoint || {};
	$.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

	var attachHandlers = function (context) {
	    $(context.contentHolderId).on("click", ".restore-link[file-version]", function (e) {
			e.preventDefault();
			$(context.contentHolderId).hide();
			$(context.confirmHolderId).show();
			context.version = $(this).attr("file-version");
		});

		$(".do-restore", context.confirmHolderId).click(function (e) {
			e.preventDefault();
			restore(context);
		});

		$(".cancel-restore", context.confirmHolderId).click(function (e) {
			e.preventDefault();
			$(context.contentHolderId).show();
			$(context.confirmHolderId).hide();
			context.version = null;
		});
	},
	restore = function (context) {
		$.telligent.evolution.put({
			url: context.restoreFileUrl,
			data: {
				fileVersion: context.version
			},
			success: function (response) {
				if (response && response.valid) {
					window.parent.jQuery.glowModal.opener(window).jQuery.glowModal.close();
				}
			},
			error: function (xhr, desc, ex) {
				window.console.log(ex);
				$(context.contentHolderId).show();
				$(context.confirmHolderId).hide();
			}
		});
	};

	$.telligent.sharepoint.widgets.versioning = {
		register: function (context) {
			attachHandlers(context);
		}
	};
})(jQuery);
