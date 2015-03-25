using System;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls
{
    [DefaultProperty("ResourceName"), ToolboxData(@"<{0}:ResourceControl runat=""server"" ResourceName="""" ResourceFile=""""></{0}:ResourceControl>")]
    public class ResourceControl : Literal
    {
        [Bindable(true), 
        Category("Appearance"), 
        DefaultValue(""),
        Description("The Key of the resource text to get")]
        public string ResourceName{get;set;}

        [Bindable(true),
        Category("Appearance"),
        DefaultValue(""),
        Description("The Name of the resource file")]
        public string ResourceFile {get;set;}

        protected override void Render(HtmlTextWriter output)
        {
            if (!String.IsNullOrEmpty(ResourceName) && !String.IsNullOrEmpty(ResourceFile))
            {
                Text = ResourceManager.GetString(ResourceName, ResourceFile);
            }
            base.Render(output);
        }
    }
}
