using System;
using System.Collections.Generic;
using System.Web.UI.HtmlControls;

namespace Telligent.Evolution.Extensions.OpenSearch.Controls.Layout
{
    public class TitleLayout : LayoutFactory
    {
        public override void Header(ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlTableCell cell = new HtmlTableCell { InnerHtml = itemInfo.Text };
            cell.Attributes["order"] = itemInfo.Order.ToString();
            cell.Attributes["style"] = itemInfo.Style;
            cell.Attributes["class"] = itemInfo.CssClass;
            cellsCollection.Add(cell);
        }

        public override void Content(string value, ItemCollectionAttribute itemInfo, List<HtmlTableCell> cellsCollection)
        {
            HtmlTableCell cell = new HtmlTableCell { InnerHtml = value };
            cell.Attributes["order"] = itemInfo.Order.ToString();
            cell.Attributes["style"] = itemInfo.Style;
            cell.Attributes["class"] = itemInfo.CssClass;
            cellsCollection.Add(cell);

            if (itemInfo.Filtered)
            {
                HtmlInputHidden hidden = new HtmlInputHidden();
                hidden.Attributes["target"] = "search-terms";
                hidden.Value = value;
                cell.Controls.Add(hidden);
            }
        }
    }
}
