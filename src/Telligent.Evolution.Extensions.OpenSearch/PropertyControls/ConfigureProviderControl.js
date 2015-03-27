function ProviderChange(dropDownList)
{
    var isVisible = jQuery(dropDownList.options[dropDownList.selectedIndex]).attr('moreresults');
    if (isVisible=='False'){
        jQuery('div#showMore').hide('slow');
    }
    else{
        jQuery('div#showMore').show('slow');
    }
}