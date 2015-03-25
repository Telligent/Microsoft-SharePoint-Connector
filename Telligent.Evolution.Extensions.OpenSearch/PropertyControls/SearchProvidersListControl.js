function AddProvider(data) {
    if (data != '' & data != null) {
        jQuery('input[id$="ProviderAction"]').val("add");
        jQuery('input[id$="ProviderData"]').val(data);
        theForm.submit();
    }
}

function DeleteProvider(data) {
    if (confirm("Are you sure that you want to delete this item?")) {
        jQuery('input[id$="ProviderAction"]').val("delete");
        jQuery('input[id$="ProviderData"]').val(data);
        theForm.submit();
    }
}

function DeleteSelected($selector) {
    if (confirm("Are you sure that you want to delete all selected items?")) {
        var itemsCollection = '';
        $selector.find('.scrollable-content').find('.check-item').each(function () {
            if (this.checked) {
                itemId = jQuery(this).attr('itemid');
                itemsCollection += itemId + '&';
            }
        });
        jQuery('input[id$="ProviderAction"]').val("delete");
        jQuery('input[id$="ProviderData"]').val(itemsCollection);
        theForm.submit();
    }
}