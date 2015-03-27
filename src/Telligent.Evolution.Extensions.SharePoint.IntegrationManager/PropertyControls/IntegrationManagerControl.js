function AddManager(data) {
    if (data != '' & data != null) {
        jQuery('input[id$="ManagerAction"]').val("add");
        jQuery('input[id$="ManagerData"]').val(data);
        theForm.submit();
    }
}

function DeleteManager(data) {
    if (confirm("Are you sure that you want to delete this item?")) {
        jQuery('input[id$="ManagerAction"]').val("delete");
        jQuery('input[id$="ManagerData"]').val(data);
        theForm.submit();
    }
}

function DeleteSelectedSPObjecManager($selector) {
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
        theForm.submit();
    }
}