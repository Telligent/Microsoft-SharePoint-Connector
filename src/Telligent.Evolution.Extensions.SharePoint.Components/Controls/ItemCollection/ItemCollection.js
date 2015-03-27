(function ($) {
	if (typeof $.telligent === 'undefined')
		$.telligent = {};

	if (typeof $.telligent.evolution === 'undefined')
		$.telligent.evolution = {};

	if (typeof $.telligent.evolution.extensions === 'undefined')
		$.telligent.evolution.extensions = {};

	if (typeof $.telligent.evolution.extensions.itemcollectioncontrol === 'undefined')
		$.telligent.evolution.extensions = {};

	if (typeof String.prototype.startsWith != 'function') {
		String.prototype.startsWith = function (str) {
			return this.indexOf(str) == 0;
		};
	}

	if (typeof String.prototype.trim != 'function') {
		String.prototype.trim = function () {
			return this.replace(/^\s*([\S\s]*)\b\s*$/, '$1');
		};
	}

	var $contextControl,
	init = function (context) {
		$contextControl = jQuery(context.controlId);
		updatePluginLayout();
		alternateRows(context);
		attachHandlers(context);
	},
	updatePluginLayout = function () {
		var selector = $contextControl.closest('table');
		jQuery('tr.field-item>td:first', selector).remove();
		jQuery(selector).siblings('div.CommonFormDescription').remove();
	},
	alternateRows = function (context) {
		var rows = jQuery('.content', context.controlId).find('tr:visible');
		for (var i = 0; i < rows.length; i++) {
			if (i % 2 != 0) {
				if (!jQuery(rows[i]).hasClass('alt'))
					jQuery(rows[i]).addClass('alt');
			}
			else {
				if (jQuery(rows[i]).hasClass('alt'))
					jQuery(rows[i]).removeClass('alt');
			}
		}
	},
	filterItems = function (context, input) {
		var $filterInput = jQuery('.header', context.controlId).find('input.plugin-filter');
		var tokenText = $filterInput.val().replace($filterInput.attr('data-placeholder'), '');

		jQuery('.content', context.controlId).find('tr').each(function () {
			var target = jQuery(this).find("input[target='search-terms']").val().trim().toUpperCase();
			var source = input.trim().toUpperCase();
			if (target.startsWith(source)) {
				jQuery(this).show();
			}
			else {
				jQuery(this).hide();
			}
		});
	},
	attachHandlers = function (context) {
		// Table rows hover
		jQuery('.content', context.controlId).find('tr').hover(function () {
			jQuery(this).addClass('hover');
			jQuery('.hover-button', this).show();
		}, function () {
			jQuery(this).removeClass('hover');
			jQuery('.hover-button', this).hide();
		});
		// Check clicked items
		jQuery('.content', context.controlId).find('tr').click(function (event) {
			if ($(event.target).is(':not(input,a)')) {
				jQuery(this).find('.check-item').each(function () {
					this.checked = !this.checked;
				});
			}
		});
		// Check all items
		jQuery('.header', context.controlId).find('input.check-item').change(function () {
			var isChecked = this.checked;
			jQuery('.content', context.controlId).find('input.check-item').each(function () {
				this.checked = isChecked;
			});
		});
		// Filter functionality
		jQuery('.header', context.controlId).find('input.plugin-filter').bind('keypress', function (e) {
			if (e.which === 13) {
				e.stopPropagation();
				e.preventDefault();
				return false;
			}
		})
		.bind('keyup', function (e) {
			e.stopPropagation();
			filterItems(context, this.value);
			alternateRows(context);
		});
		jQuery('.header', context.controlId).find('input.plugin-filter')
		.focusin(function () {
			if ($(this).hasClass("empty")) {
				$(this).removeClass("empty");
			}
			var tokenText = $(this).val().replace($(this).attr('data-placeholder'), '');
			$(this).val(tokenText);
		})
		.focusout(function () {
			if ($(this).val() == '') {
				$(this).val($(this).attr('data-placeholder'));
				if (!$(this).hasClass("empty")) {
					$(this).addClass("empty");
				}
			}
		})
		.trigger('focusout');
	};
	$.telligent.evolution.extensions.itemcollectioncontrol = {
		register: function (context) {
			init(context);
			//context.filterInput
		}
	};
})(jQuery);