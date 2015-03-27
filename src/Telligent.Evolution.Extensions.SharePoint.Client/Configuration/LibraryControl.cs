using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public class LibraryControl : ListControl
    {
        protected override string Title
        {
            get { return version2.SharePointLibraryExtension.Plugin.SPDocLibMsg(); }
        }
        
        override protected string GetInitialClientScript()
        {
            return @"jQuery(document).ready(function(){
                        jQuery.telligent.evolution.extensions.lookupSharePointList.register({
                            WebItemControl: jQuery('input#web-item-url'),
                            LookUpTextBox: jQuery('input#list-item-id'),
                            Spinner: '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></div>',
                            Loader: '<span style=""margin: 4px;"" class=""loading""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></span>',
                            listType: 'DocumentLibrary'
                        });
                    });";
        }

    }
}
