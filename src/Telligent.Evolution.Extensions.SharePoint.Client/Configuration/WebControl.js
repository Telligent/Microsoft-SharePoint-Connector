(function ($) {
	if (typeof $.telligent === 'undefined')
		$.telligent = {};

	if (typeof $.telligent.evolution === 'undefined')
		$.telligent.evolution = {};

	if (typeof $.telligent.evolution.extensions === 'undefined')
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

	var spinner,
	InvokeSearchSPLists = function (context, textbox, searchText) {
		if (searchText && searchText.length >= 1) {
			textbox.glowLookUpTextBox('updateSuggestions', [textbox.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)]);
			textbox.glowLookUpTextBox('updateSuggestions',
				$.map(context.Data, function (site, i) {
					if (site.Name.toUpperCase().startsWith(searchText.trim().toUpperCase())) {
						var markup = site.Name + "<div style='white-space: nowrap; text-overflow: ellipsis; overflow: hidden; font-size: 11px;color: #777;'>" + site.Url + "</div>";
						return textbox.glowLookUpTextBox('createLookUp', site.Url, site.Name, markup, true);
					}
				})
			);
		}
	},
	searchSPList = function (context) {
		var siteName = [];
		var siteUrl = context.LookUpTextBox.val();
		if (siteUrl != "") {
			for (var i = 0; i < context.Data.length; i++) {
				if (context.Data[i].Url == siteUrl) {
					siteName[0] = context.Data[i].Name;
					break;
				}
			}
		}
		context.LookUpTextBox.glowLookUpTextBox({
			delimiter: ',',
			allowDuplicates: true,
			maxValues: 1,
			onGetLookUps: function (tb, searchText) {
				InvokeSearchSPLists(context, tb, searchText);
			},
			emptyHtml: '',
			selectedLookUpsHtml: siteName,
			deleteImageUrl: ''
		});
	};
	$.telligent.evolution.extensions.lookupSharePointWeb = {
		register: function (context) {
			spinner = context.Spinner;
			searchSPList(context);
		}
	};
})(jQuery);