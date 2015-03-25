(function ($) {
    $.telligent = $.telligent || {};
    $.telligent.sharepoint = $.telligent.sharepoint || {};
    $.telligent.sharepoint.controlPanel = $.telligent.sharepoint.controlPanel || {};

    // Markup example
    // <table class="site-user-profile tbl-field-mapping">
    // ...
    // <td class="sp-field-cell"><select>...</select></td>
    // <td class="action-cell"><select>...</select></td>
    // <td class="te-field-cell"><select>...</select></td>
    // <td class="del-cell">...</td>
    // ...
    // <a class="add-row-link" href="#">Add another user profile attribute...</a>
    // ...
    // </table>

    var defaultOptions = {
        wrapper: document,
        siteProfileFieldsMapping: null,
        farmProfileFieldsMapping: null,
        farmSyncCheckBox: null,
        errorHolder: null,
        errorMessage: ''
    },
    init = function (context) {

        if ($(context.farmSyncCheckBox).is(':checked')) {
            $("table.site-user-profile.tbl-field-mapping", context.wrapper).hide();
            $("table.farm-user-profile.tbl-field-mapping", context.wrapper).show();
        } else {
            $("table.site-user-profile.tbl-field-mapping", context.wrapper).show();
            $("table.farm-user-profile.tbl-field-mapping", context.wrapper).hide();
        }

        refresh(
            $("table.site-user-profile.tbl-field-mapping", context.wrapper),
            $.parseJSON(context.siteProfileFieldsMapping.val())
        );

        refresh(
            $("table.farm-user-profile.tbl-field-mapping", context.wrapper),
            $.parseJSON(context.farmProfileFieldsMapping.val())
        );

    },
    attachHandlers = function (context) {
        $("a.add-row-link", context.wrapper).click(function (e) {
            e.preventDefault();
            var $tbl = $(this).closest("table");
            var $newtr = addNewRow($tbl);
            fillActionList($newtr, true);
            validate(context);
        });

        $("td.sp-field-cell select", context.wrapper).live("change", function () {
            var $tr = $(this).closest("tr");
            var allowImport = ($(this.options[this.selectedIndex]).attr("noimp") != "1");
            fillActionList($tr, allowImport);
            refreshSPSelectors($tr.closest("tbody"));
            validate(context);
        });

        $("td.te-field-cell select", context.wrapper).live("change", function () {
            refreshTESelectors($(this).closest("tbody"));
            validate(context);
        });

        $("td.del-cell", context.wrapper).live("click", function (e) {
            e.preventDefault();
            var $tbl = $(this).closest("tbody");
            $(this).closest("tr").remove();
            if ($("tr:not(.template)", $tbl).length == 0) {
                addNewRow($tbl);
            }
            validate(context);
        });

        $(context.farmSyncCheckBox).change(function () {
            if ($(this).is(':checked')) {
                $("table.site-user-profile.tbl-field-mapping", context.wrapper).hide();
                $("table.farm-user-profile.tbl-field-mapping", context.wrapper).show();
            } else {
                $("table.site-user-profile.tbl-field-mapping", context.wrapper).show();
                $("table.farm-user-profile.tbl-field-mapping", context.wrapper).hide();
            }
            validate(context);
        });

        $(".save-button").click(function (e) {
            var isValid = validate(context);
            if (!isValid) {
                e.stopPropagation();
                return false;
            }
            return true;
        });
    },
    isValid = function (siteMapping, farmMapping, farmSyncEnabled) {
        return true;
        return ((farmSyncEnabled && farmMapping.length > 0) || (!farmSyncEnabled && siteMapping.length > 0));
    },
    validate = function (context) {
        var siteMapping = getData($("table.site-user-profile.tbl-field-mapping tbody", context.wrapper));
        context.siteProfileFieldsMapping.val(JSON.stringify(siteMapping));

        var farmMapping = getData($("table.farm-user-profile.tbl-field-mapping tbody", context.wrapper));
        context.farmProfileFieldsMapping.val(JSON.stringify(farmMapping));

        var valid = isValid(siteMapping, farmMapping, $(context.farmSyncCheckBox).is(':checked'));
        if (valid) {
            $(context.errorHolder).hide();
        } else {
            $(context.errorHolder).text(context.errorMessage).show();
        }
        return valid;
    },
    getData = function ($tbl) {
        var data = [];
        $("tr:not(.template)", $tbl).map(function (i, tr) {
            var spField = $(tr).find("td.sp-field-cell select").val();
            var teField = $(tr).find("td.te-field-cell select").val();
            var syncDir = $(tr).find("td.action-cell select").val();
            if (spField != null && spField != ''
             && teField != null && teField != ''
             && syncDir != null && syncDir != '') {
                data.push({
                    ExternalUserFieldId: spField,
                    InternalUserFieldId: teField,
                    SyncDirection: syncDir
                });
            }
        });
        return data;
    },
    addNewRow = function ($tbl) {
        var $trTmp = $tbl.find("tr.template");
        var $newtr = $trTmp.clone().removeClass("template");
        $newtr.find("select").removeAttr("id").removeAttr("name");
        $trTmp.before($newtr);
        refreshSPSelectors($tbl);
        refreshTESelectors($tbl);
        return $newtr;
    },
    refresh = function ($tbl, fields) {
        if (!fields) {
            addNewRow($tbl);
        }

        for (var i = 0; fields && i < fields.length; i++) {
            var field = fields[i];
            var $newtr = addNewRow($tbl);
            var selSp = $newtr.find("td.sp-field-cell select").val(field.ExternalUserFieldId)[0];
            $newtr.find("td.te-field-cell select").val(field.InternalUserFieldId);
            var allowImport = ($(selSp.options[selSp.selectedIndex]).attr("noimp") != "1");
            fillActionList($newtr, allowImport);
            $newtr.find("td.action-cell select").val(field.SyncDirection);
        }
    },
    refreshTESelectors = function ($tbl) {
        refreshSelectors($tbl.find("tr:not(.template) td.te-field-cell select"), $tbl.find("tr.template td.te-field-cell select")[0].options);
    },
    refreshSPSelectors = function ($tbl) {
        refreshSelectors($tbl.find("tr:not(.template) td.sp-field-cell select"), $tbl.find("tr.template td.sp-field-cell select")[0].options);
    },
    refreshSelectors = function ($selectors, selOptions, $selToRefresh) {
        if (!$selToRefresh || typeof $selToRefresh === "undefined") {
            $selToRefresh = $selectors;
        }

        if ($selToRefresh.length > 1) {
            $selToRefresh.each(function () {
                var selVal = this.value;
                this.options.length = 0;
                for (var i = 0, len = selOptions.length; i < len; i++) {
                    var opt = selOptions[i];
                    if (opt.value != "" && (opt.value == selVal || $selectors.filter("[value='" + opt.value + "']").length == 0)) {
                        var newOption = new Option(opt.text, opt.value)
                        if ($(opt).attr("noimp") == "1") {
                            $(newOption).attr("noimp", "1");
                        }
                        $(newOption).attr("title", $(opt).attr("title"));
                        this.options.add(newOption);
                    }
                }
                this.value = selVal;
            });
        }
    },
    fillActionList = function ($tr, allowImport) {
        var selAction = $tr.find(".action-cell select")[0];
        if (allowImport) {
            selAction.options.length = 0;
            selAction.options.add(new Option("<---- Import", "1"));
            selAction.options.add(new Option("Export ---->", "2"));
        }
        else {
            selAction.options.length = 0;
            selAction.options.add(new Option("Export ---->", "2"));
        }
    };

    $.telligent.sharepoint.controlPanel.profileSync = {
        register: function (context) {
            var options = $.extend({}, defaultOptions, context || {});
            init(options);
            attachHandlers(options);
        }
    };
})(jQuery);