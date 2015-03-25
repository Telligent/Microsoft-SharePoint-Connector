(function ($) {
	var performSearch = function (context, ajaxQuery) {
		$.telligent.evolution.get({
			url: context.searchUrl,
			data: ajaxQuery,
			timeout: context.timeout,
			success: function (response) {
				context.resultHolder.html(response);
			},
			error: function (jqXHR) {
				$.telligent.evolution.get({
					url: context.error,
					success: function (response) {
						context.resultHolder.html(response);
					}
				})
			}
		});
	},
	constructAndExecuteQuery = function (context) {
		var query = constructQueryFromInput(context);
		$.hashdata(query);
	},
	// update url in according to input text
	constructQueryFromInput = function (context) {
		var query = {
			q: $(context.searchTextInput).val()
		};
		query[context.pageIndexQueryStringKey] = 1;
		return query;
	},
	getCurrentQuery = function () {
		var ajaxQuery = {};
		$.each($.hashdata(), function (key, value) {
			if (key.indexOf('pi') === 0) {
				ajaxQuery[key] = value;
			} else {
				ajaxQuery['w_' + key] = value;
			}
		});
		return ajaxQuery;
	},
	deserialize = function (hash) {
		data = {};
		pairs = hash.split('&');
		$.each(pairs, function (i, pair) {
			pair = pair.split('=');
			var value = pair.length === 2 ? pair[1] : '';
			data[pair[0]] = decodeURIComponent(value.replace(/\+/gi, ' '));
		});
		return data;
	},
	api = {
		register: function (context) {
			context.contentWrapper = $(context.contentWrapper);
			context.resultHolder = $(context.resultHolder);
			performSearch(context, getCurrentQuery());
			$(window).bind(
				'directedHashChange',
				{
					customComparer: function (prev, curr) {
						prev = deserialize(prev);
						curr = deserialize(curr);
						if (typeof prev[context.pageIndexQueryStringKey] !== 'undefined' && typeof curr[context.pageIndexQueryStringKey] !== 'undefined') {
							if (Number(prev[context.pageIndexQueryStringKey]) < Number(curr[context.pageIndexQueryStringKey])) {
								return 1;
							} else if (Number(prev[context.pageIndexQueryStringKey]) > Number(curr[context.pageIndexQueryStringKey])) {
								return -1;
							} else {
								return 0;
							}
						}
						return 0;
					}
				},
				function (e, data) {
					var data = getCurrentQuery();
					if (typeof data["w_q"] !== 'undefined') {
						performSearch(context, data);
					}
				}
			);
			// handle enter presses in search box
			$(context.searchTextInput).live('keyup', function (e, data) {
				if (e.which === 13) {
					constructAndExecuteQuery(context);
				}
			});
			// handle search button clicks
			$(context.searchButton).live('click', function (e, data) {
				constructAndExecuteQuery(context);
				return false;
			});
			// handle pagination link clicks
			$('#' + context.wrapperId + ' div.open-search-summary div.os-content-list-footer a').live('click', function (e, data) {
				var page = Number($(this).attr('href').split('=')[1].split('#')[0]);
				var paginationData = {};
				paginationData[context.pageIndexQueryStringKey] = page;
				$.hashdata(paginationData, true);
				e.stopPropagation();
				return false;
			});
		}
	};
	if (typeof $.telligent === 'undefined') { $.telligent = {}; }
	if (typeof $.telligent.evolution === 'undefined') { $.telligent.evolution = {}; }
	if (typeof $.telligent.evolution.widgets === 'undefined') { $.telligent.evolution.widgets = {}; }
	$.telligent.evolution.widgets.externalSearchSummaryResults = api;
} (jQuery));