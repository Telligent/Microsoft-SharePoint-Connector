(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.widgets = $.telligent.sharepoint.widgets || {};

    var speed = 100,
    defaultOptions = {
        webUrl: null,
        sspId: null,
        termSetId: null,
        termSetUrl: null,
        termSetTemplateId: null,
        termSetContentId: null,
        selectedTerms: null,
        selectedTermsContentId: null,
        termItemTemplateId: null,
        selectButtonId: null,
        allowMultipleValues: false,
        keywordsHolderId: null,
        keywordsUrl: null
    },
    init = function (context) {
        if (context.termSetId) {
            initTermSet(context);
            attachTermSetHandlers(context);
        }
        context.termItemTemplate = $.telligent.evolution.template.compile(context.termItemTemplateId);
        for (var i = 0, len = context.selectedTerms.length; i < len; i++) {
            renderSelectedTerm(context, context.selectedTerms[i]);
        }
    },
    initTermSet = function (context) {
        context.termSetTemplate = $.telligent.evolution.template.compile(context.termSetTemplateId);
        var $content = $("#" + context.termSetContentId).addClass("loading");
        $.telligent.evolution.get({
            url: context.termSetUrl,
            data: {
                url: context.webUrl,
                sspId: context.sspId,
                termSetId: context.termSetId
            },
            success: function (terms) {
                $("#" + context.termSetContentId).html(context.termSetTemplate(terms));
            },
            complete: function () {
                $content.removeClass("loading");
            }
        });
    },
    attachTermSetHandlers = function (context) {
        $("#" + context.termSetContentId).on("click", ".expand-collapse.haschilds", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var self = $(this).closest(".term-item[data-termId]");

            var status = self.attr('status');
            if (status !== "completed") {
                self.addClass('loading');
                $.telligent.evolution.get({
                    url: context.termSetUrl,
                    data: {
                        url: context.webUrl,
                        sspId: context.sspId,
                        termSetId: context.termSetId,
                        termId: self.data("termid")
                    },
                    success: function (terms) {
                        $(this).addClass('expanded');
                        $(".content", self).html(context.termSetTemplate(terms)).slideDown(speed);
                    },
                    complete: function () {
                        self.removeClass('loading');
                    }
                });
                self.attr('status', 'completed');
            }

            if ($(this).hasClass('expanded')) {
                $(this).removeClass('expanded').addClass('collapsed');
                $(".content", self).slideUp(speed);
            }
            else {
                $(this).addClass('expanded').removeClass('collapsed');
                $(".content", self).slideDown(speed);
            }
        }).on("click", ".term-item[data-termId]", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var self = $(this).closest(".term-item[data-termId]");
            $("#" + context.termSetContentId).find('a.selected').removeClass('selected');
            $('a:first', self).addClass("selected");
            context.term = {
                wssId: self.data("wssid"),
                id: self.data("termid"),
                name: self.data("termname")
            };
        });
    },
    attachHandlers = function (context) {
        $("#" + context.selectedTermsContentId).on("click", ".term-item .remove", function (e) {
            e.preventDefault();
            $(this).closest(".term-item").remove();
        });

        $("#" + context.selectButtonId).click(function (e) {
            e.preventDefault();
            if (context.termSetId && context.term) {
                var hasDuplicates = $("#" + context.selectedTermsContentId).find(".term-item[data-termId='" + context.term.id + "']").length > 0;
                if (!hasDuplicates) {
                    renderSelectedTerm(context, context.term);
                }
            }
            else {
                var $keywords = $("#" + context.keywordsHolderId),
                    keywords = $keywords.val();
                if (keywords && keywords.length > 0) {
                    $keywords.attr('disabled', 'disabled');
                    $.telligent.evolution.put({
                        url: context.keywordsUrl,
                        data: {
                            url: context.webUrl,
                            labels: keywords
                        },
                        success: function (res) {
                            for (var i = 0, len = res.terms.length; i < len; i++) {
                                var term = res.terms[i],
                                    hasDuplicates = $("#" + context.selectedTermsContentId).find(".term-item[data-termId='" + term.id + "']").length > 0;
                                if (!hasDuplicates) {
                                    renderSelectedTerm(context, term);
                                }
                            }
                        },
                        complete: function () {
                            $keywords.removeAttr('disabled').val("");
                        }
                    });
                }
            }
        });
    },
    renderSelectedTerm = function (context, term) {
        var $content = $("#" + context.selectedTermsContentId);
        if (!term.wssId) {
            term.wssId = -1;
        }
        var termItemHtml = context.termItemTemplate({
            term: term
        });
        if (context.allowMultipleValues) {
            $content.append(termItemHtml);
        }
        else {
            $content.html(termItemHtml);
        }
    };

    $.telligent.sharepoint.widgets.taxonomies = {
        register: function (options) {
            var context = $.extend({}, defaultOptions, options);
            init(context);
            attachHandlers(context);
        }
    };
})(jQuery);
