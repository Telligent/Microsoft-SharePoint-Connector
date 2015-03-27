using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;

namespace Telligent.Evolution.Extensions.OpenSearch.Controls.Layout
{
    public class SubTitleLayout : LayoutFactory
    {
        public override void Header(ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlTableCell tableCell = cellsCollection.Last(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
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
            if (String.IsNullOrEmpty(value))
            {
                return;
            }
            HtmlGenericControl subTitle = new HtmlGenericControl("div") { InnerHtml = value };
            subTitle.Attributes["style"] = itemInfo.Style;
            subTitle.Attributes["class"] = itemInfo.CssClass;
            HtmlTableCell tableCell = cellsCollection.LastOrDefault(cell => cell.Attributes["order"] == itemInfo.Order.ToString());
            if (tableCell != null)
            {
                tableCell.Controls.Add(subTitle);
            }
            else
            {
                HtmlTableCell cell = new HtmlTableCell { InnerHtml = itemInfo.Text };
                cell.Attributes["order"] = itemInfo.Order.ToString();
                cell.Attributes["style"] = itemInfo.Style;
                cell.Attributes["class"] = itemInfo.CssClass;
                cell.Controls.Add(subTitle);
                cellsCollection.Add(cell);
            }
        }
    }
}
