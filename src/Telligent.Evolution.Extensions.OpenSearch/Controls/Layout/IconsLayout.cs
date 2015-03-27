using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;

namespace Telligent.Evolution.Extensions.OpenSearch.Controls.Layout
{
    public class IconsLayout : LayoutFactory
    {
        public override void Header(ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell == null)
            {
                HtmlTableCell cell = new HtmlTableCell { InnerHtml = itemInfo.Text };
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cell.Attributes["style"] = itemInfo.Style;
                cell.Attributes["class"] = itemInfo.CssClass;
                cellsCollection.Add(cell);
            }
        }

        public override void Content(string value, ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlGenericControl subTitle = new HtmlGenericControl("span");
            subTitle.Attributes["style"] = itemInfo.Style;
            subTitle.Attributes["class"] = value;
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell != null)
            {
                tableCell.Controls.Add(subTitle);
                tableCell.Attributes["class"] = String.Format("{0} {1}", tableCell.Attributes["class"],itemInfo.CssClass).Trim();
            }
            else
            {
                HtmlTableCell cell = new HtmlTableCell { InnerHtml = itemInfo.Text };
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cell.Attributes["class"] = itemInfo.CssClass;
                cell.Controls.Add(subTitle);
                cellsCollection.Add(cell);
            }
        }
    }
}
