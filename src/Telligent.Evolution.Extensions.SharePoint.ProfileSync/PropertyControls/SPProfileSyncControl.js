﻿function AddSyncSettings(data) {
    if (data != '' & data != null) {
        jQuery('input[id$="ManagerAction"]').val("add");
        jQuery('input[id$="ManagerData"]').val(data);
        jQuery('form:first').submit();
    }
}

function DeleteSyncSettings(data) {
    if (confirm("Are you sure that you want to delete this item?")) {
        jQuery('input[id$="ManagerAction"]').val("delete");
        jQuery('input[id$="ManagerData"]').val(data);
        jQuery('form:first').submit();
    }
}

function DeleteSelectedSyncSettings($selector) {
    if (confirm("Are you sure that you want to delete all selected items?")) {
        var itemsCollection = '';
        $selector.find('.scrollable-content').find('.check-item').each(function () {
            if (this.checked) {
                itemId = jQuery(this).attr('itemid');
                itemsCollection += itemId + '&';
            }
        });
        jQuery('input[id$="ManagerAction"]').val("delete");
        jQuery('input[id$="ManagerData"]').val(itemsCollection);
        jQuery('form:first').submit();
    }
}