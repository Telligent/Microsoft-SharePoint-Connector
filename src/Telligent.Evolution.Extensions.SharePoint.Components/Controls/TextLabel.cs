using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Telligent.Evolution.Components;
using System.ComponentModel;
using System.Web.UI;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls
{
    [DefaultProperty("Key"), ToolboxData(@"<{0}:TextLabel runat=""server"" ResourceName="""" ResourceFile=""""></{0}:TextLabel>")]
    public class TextLabel : System.Web.UI.WebControls.Label
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
                this.Text = ResourceManager.GetString(ResourceName, ResourceFile);
            }
            base.Render(output);
        }
    }
}
