using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web;

namespace Telligent.Evolution.Extensions.OpenSearch.Controls.Layout
{
    public class HoverButtonsLayout : LayoutFactory
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

        public override void Content(string value, ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlGenericControl hoverBtn = new HtmlGenericControl("a") { InnerHtml = itemInfo.Text };
            hoverBtn.Attributes["href"] = HttpUtility.HtmlDecode(value);
            hoverBtn.Attributes["style"] = String.Format("display: none; {0}", itemInfo.Style);
            hoverBtn.Attributes["class"] = String.Format("hover-button {0}", itemInfo.CssClass).Trim();
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell != null)
            {
                tableCell.Controls.Add(hoverBtn);
            }
            else
            {
                HtmlTableCell cell = new HtmlTableCell();
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cell.Attributes["class"] = "hover-buttons-column";
                cell.Controls.Add(hoverBtn);
                cellsCollection.Add(cell);
            }
        }
    }
}
