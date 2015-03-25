(function ($, global) {

	if (typeof $.telligent === 'undefined') { $.telligent = {}; }
	if (typeof $.telligent.evolution === 'undefined') { $.telligent.evolution = {}; }
	if (typeof $.telligent.evolution.widgets === 'undefined') { $.telligent.evolution.widgets = {}; }

	var _save = function (context) {
		context.successMessage.hide();
		context.moderateMessage.hide();
		context.errorMessage.hide();
		var w = $('#' + context.wrapperId);

		context.save.html('<span></span>' + context.publishingText).addClass('disabled');

		$.telligent.evolution.post({
			url: context.addCommentFormUrl,
			data: {
				Comment: $(context.bodySelector).evolutionComposer('val'),
				ContentId: context.contentId,
				ContentTypeId: context.contentTypeId
			},
			success: function (response) {
				$('.processing', w).css('visibility', 'hidden');
				context.successMessage.slideDown();
				global.setTimeout(function () { context.successMessage.fadeOut().slideUp(); }, 9999);

				$(document).trigger('sharepoint_documentlibrary_commentposted', '');

				$(context.bodySelector).evolutionComposer('val', '');
				$(context.bodySelector).change();
				context.save.evolutionValidation('reset');
				context.save.html('<span></span>' + context.publishText).removeClass('disabled');
			},
			error: function (xhr, desc, ex) {
				$('.processing', w).css("visibility", "hidden");
				context.save.html('<span></span>' + context.publishText).removeClass('disabled');
				context.errorMessage.html(context.publishErrorText + ' (' + desc + ')').slideDown();
			}
		});
	};

	$.telligent.evolution.widgets.documentLibraryPostCommentForm = {
		register: function (context) {
			if (document.URL.indexOf('#addcomment') >= 0) {
				$(context.bodySelector).focus();
			}
			$('.internal-link.close-message', $('#' + context.wrapperId)).click(function () {
				$(this).blur();
				context.successMessage.fadeOut().slideUp();
				return false;
			});

			var body = $(context.bodySelector);
			body.one('focus', function () {
				body.evolutionComposer({
					plugins: ['mentions', 'hashtags']
				});
			});

			context.save
				.evolutionValidation({
					onValidated: function (isValid, buttonClicked, c) {
						if (isValid) {
							context.save.removeClass('disabled');
						} else {
							context.save.addClass('disabled');
						}
					},
					onSuccessfulClick: function (e) {
						e.preventDefault();
						$('.processing', context.save.parent()).css("visibility", "visible");
						context.save.addClass('disabled');
						_save(context);
					}
				})
				.evolutionValidation('addField', context.bodySelector, {
					required: true,
					maxlength: 1000000,
					messages:
					{
						required: context.bodyRequiredText
					}
				}, '#' + context.wrapperId + ' .field-item.post-body .field-item-validation', null);
		}
	};

})(jQuery, window);
