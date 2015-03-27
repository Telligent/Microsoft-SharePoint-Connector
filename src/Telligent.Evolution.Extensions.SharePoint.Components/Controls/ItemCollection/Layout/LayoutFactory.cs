using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout
{
    public abstract class LayoutFactory
    {
        public abstract void Header(ItemCollectionAttribute itemInfo,List<HtmlTableCell> cellsCollection);
        public abstract void Content(object value, ItemCollectionAttribute itemInfo,List<HtmlTableCell> cellsCollection);

        public static void DrawHeader(ItemCollectionAttribute itemInfo,List<HtmlTableCell> cellsCollection)
        {
            LayoutFactory layoutFactory = GetLayoutFactory(itemInfo.Region);
            if (layoutFactory != null)
                layoutFactory.Header(itemInfo, cellsCollection);
        }

        public static void DrawContent(object value, ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            LayoutFactory layoutFactory = GetLayoutFactory(itemInfo.Region);
            if (layoutFactory != null)
                layoutFactory.Content(value, itemInfo, cellsCollection);
        }

        private static LayoutFactory GetLayoutFactory(Region region)
        {
            var layoutAttribute = typeof(Region).GetField(region.ToString()).GetCustomAttributes(typeof(LayoutAttribute), false).FirstOrDefault();
            if (layoutAttribute == null)
                return null;
            return ((LayoutAttribute)layoutAttribute).Layout;
        }
    }
}
