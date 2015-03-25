using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout
{
    public class CustomLayout : LayoutFactory
    {
        public override void Header(ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell == null)
            {
                HtmlTableCell cell = new HtmlTableCell { InnerHtml = itemInfo.Text };
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cellsCollection.Add(cell);
            }
        }

        public override void Content(object value, ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlGenericControl customLayout = (HtmlGenericControl)value;
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell != null)
            {
                tableCell.Controls.Add(customLayout);
            }
            else
            {
                HtmlTableCell cell = new HtmlTableCell();
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cell.Attributes["class"] = itemInfo.CssClass;
                cell.Controls.Add(customLayout);
                cellsCollection.Add(cell);
            }
        }
    }
}
