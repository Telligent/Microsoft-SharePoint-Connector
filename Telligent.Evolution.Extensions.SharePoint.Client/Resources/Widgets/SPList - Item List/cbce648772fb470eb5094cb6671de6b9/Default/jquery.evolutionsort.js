(function ($) {
    var convertToList = function (select, settings) {
        var list = $('<ul></ul>'), prevProp = null;
        select.find('option').each(function (i) {
            var opt = $(this),
				prop = opt.attr('value'),
				direction = opt.attr('data-direction'),
				item = $('<a href="#" />')
					.html("<span></span>" + opt.html())
					.addClass(direction)
					.wrap('<li />')
					.parent()
					.appendTo(list);
            if (opt.attr('selected') === 'selected') {
                item.addClass('selected');
            }
            if (prevProp !== null && prevProp === prop) {
                item.addClass('secondary');
            }
            prevProp = prop;
            item.find('a').bind('click', function (e) {
                e.preventDefault();
                list.find('li').removeClass('selected');
                item.addClass('selected');
                if (opt.attr('data-query')) {
                    window.location = opt.attr('data-query');
                } else {
                    select.trigger(settings.eventName, {
                        prop: prop,
                        direction: direction,
                        index: i
                    });
                }
            });

        });
        var container = select.wrap('<div />').parent().css({ position: 'relative' });
        list = list.wrap('<div class="' + settings.cssClass + '" />')
			.parent()
			.hide()
			.css({ position: 'absolute', top: 0, left: 0, zIndex: settings.zIndex })
			.appendTo(container);
        container.width(list.width()+"px");
        return list;
    };
    $.fn.evolutionSort = function (options) {
        var settings = $.extend({}, $.fn.evolutionSort.defaults, options || {}),
			selection = this;

        // convert select into sorter
        var list = convertToList(selection, settings);

        // set up initial selected state
        var currentSelection = list.find('li a:first').clone(), currentSelectionIndex = 0;
        list.find('li').each(function (i) {
            if ($(this).is('.selected')) {
                currentSelectionIndex = i;
                currentSelection = $(this).find('a:first').clone();
            }
        });
        selection.after(currentSelection);

        // handle events
        list.bind('mouseleave', function () {
            list.fadeOut(settings.duration);
        });
        list.find('a').bind('click', function () {
            currentSelection.html($(this).html());
            list.fadeOut(settings.duration);
        });
        selection.bind(settings.eventName, function (e, data) {
            if ('index' in data) {
                currentSelectionIndex = data.index;
            }
        });
        currentSelection.bind({
            mouseenter: function (e) {
                list.css({ top: (currentSelection.outerHeight() + settings.offsetHeightPadding) * currentSelectionIndex * -1 });
                list.fadeIn(settings.duration);
            },
            mouseleave: function (e) {
                if ($(e.relatedTarget).parentsUntil(selection.parent(), '.' + settings.cssClass).length === 0) {
                    list.fadeOut(settings.duration);
                }
            }
        });

        return selection;
    };
    $.fn.evolutionSort.defaults = {
        eventName: 'evolutionSort',
        cssClass: 'sorter',
        duration: 100,
        offsetHeightPadding: 0,
        zIndex: 1000
    };
})(jQuery);