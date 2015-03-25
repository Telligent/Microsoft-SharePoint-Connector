// Support for hash navigation
window.location.hash = window.location.hash;
(function ($) {
	var hashChangeSupported = typeof window.onhashchange !== 'undefined',
		lastHash = window.location.hash,
		hashChangeInterval,
		win = $(window),
		history = [window.location.hash];
	backButtonIndex = 0;
	if (hashChangeSupported) {
		$.event.special.hashchange = {
			setup: function () {
				if (!hashChangeInterval) {
					hashChangeInterval = setInterval(function () {
						if (window.location.hash !== lastHash) {
							lastHash = window.location.hash;
							win.trigger('hashchange');
						}
					}, $.hashChangeSettings.interval);
				}
			}
		};
	}
	$.event.special.directedHashChange = {
		setup: function (data) {
			var comparer = data.customComparer;
			win.bind('hashchange', function () {
				var isReverse, comparerResult = 0;
				if (!!comparer) {
					comparerResult = comparer(history[history.length - 1], window.location.hash);
					backButtonIndex = history.length;
					history.push(window.location.hash);
				}
				if (comparerResult !== 0) {
					isReverse = comparerResult < 0;
				} else {
					isReverse = (history.length >= 2 && history[backButtonIndex - 1] === window.location.hash);
				}
				if (comparerResult !== 0) {
					if (isReverse) {
						backButtonIndex--;
					} else {
						backButtonIndex++;
						if (backButtonIndex === history.length) {
							history.push(window.location.hash);
						}
					}
				}
				win.trigger('directedHashChange', {
					direction: isReverse ? 'backward' : 'forward',
					hash: window.location.hash
				});
			});
		}
	};
	$.hashChangeSettings = {
		interval: 10
	};
} (jQuery));


(function ($) {
	$.hashdata = function (data, adjustExisting) {
		if (typeof data === 'undefined') {
			data = {};
			var urlParts = window.location.href.split("#");
			// firefox workaround
			if (urlParts.length > 2) {
				var rejoinedParts = '';
				$.each(urlParts, function (i, part) {
					if (i > 0) {
						if (i > 1) {
							rejoinedParts += '#';
						}
						rejoinedParts += part;
					}
				});
				urlParts = [urlParts[0], rejoinedParts];
			}
			if (urlParts.length === 2) {
				pairs = urlParts[1].split('&');
				$.each(pairs, function (i, pair) {
					pair = pair.split('=');
					if (pair.length === 2 && pair[1].length > 0) {
						data[pair[0]] = decodeURIComponent(pair[1]).replace(/\+/gi, ' ');
					}
				});
			}
			return data;
		} else {
			if (adjustExisting) {
				data = $.extend($.hashdata(), data);
			}
			window.location.href = window.location.href.split('#')[0] + '#' + $.param(data);
		}
	};
} (jQuery));
