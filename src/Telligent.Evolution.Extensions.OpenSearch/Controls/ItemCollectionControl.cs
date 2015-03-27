using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.OpenSearch.Controls.Layout;

[assembly: WebResource("Telligent.Evolution.Extensions.OpenSearch.Controls.ItemCollection.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.OpenSearch.Controls
{
    public abstract class ItemCollectionControl : Control
    {
        public enum Header { Simple, Advanced }
        protected string BaseUrl { get; private set; }
        protected string FilterDefaultText { get; set; }
        protected abstract string CSSpath();
        protected HtmlGenericControl contentDiv = new HtmlGenericControl("div");
        protected HtmlTable itemListHeader = new HtmlTable();
        protected HtmlTable itemListContent = new HtmlTable();
        protected HtmlGenericControl scrollableItemList = new HtmlGenericControl("div");
        protected HtmlGenericControl itemListFooter = new HtmlGenericControl("div");

        protected abstract List<Control> HeaderButtons();

        #region Control Overrides
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.BaseUrl = Page.Request.Url.GetLeftPart(UriPartial.Authority);
            contentDiv.ID = "sharepoint-component";
            contentDiv.ClientIDMode = System.Web.UI.ClientIDMode.AutoID;
            contentDiv.Attributes["class"] = "sharepoint-component-itemcollection";
            itemListHeader.Attributes["class"] = "header";
            itemListContent.Attributes["class"] = "content";
            EnsureChildControls();
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            contentDiv.Controls.Add(itemListHeader);
            contentDiv.Controls.Add(scrollableItemList);
            contentDiv.Controls.Add(itemListFooter);
            scrollableItemList.Controls.Add(itemListContent);
            this.Controls.Add(contentDiv);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            this.Controls.Add(new LiteralControl(PageScripts()));
            // Register javascript
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(ItemCollectionControl),
                "Telligent.Evolution.Extensions.OpenSearch.Controls.ItemCollection.js");
        }
        #endregion

        #region JavaScript and CSS
        private string CreateJavaScript(params string[] scripts)
        {
            string csslink = String.Format("<link rel='stylesheet' type='text/css' href='{0}' />", this.CSSpath());
            string javascript = @"<script type=""text/javascript"" language=""javascript"">"
                                + String.Concat(scripts) +
                                "</script>";
            return String.Concat(csslink, javascript);
        }

        private string PageScripts()
        {
            string initBehavior = @"
jQuery(function(){
    jQuery.telligent.evolution.extensions.itemcollectioncontrol.register({
        controlId: '#" + contentDiv.ClientID + @"'
    });
});";
            return CreateJavaScript(initBehavior);
        }
        #endregion

        #region Public methods
        public void Bind<T>(List<T> itemCollection, bool showCheckers = false, Header header = Header.Advanced)
        {
            Dictionary<ItemCollectionAttribute, PropertyInfo> properties = LoadPropertiesAndTitles(typeof(T));
            switch (header)
            {
                case Header.Simple: CreateHeader(itemListHeader, properties, showCheckers); break;
                case Header.Advanced: CreateAdvancedHeader(itemListHeader, properties, showCheckers); break;
            }
            itemCollection.ForEach(item => ProviderDataBind(itemListContent, item, properties, showCheckers));
        }
        #endregion

        #region Markup
        private Dictionary<ItemCollectionAttribute, PropertyInfo> LoadPropertiesAndTitles(Type itemType)
        {
            Dictionary<ItemCollectionAttribute, PropertyInfo> properties = new Dictionary<ItemCollectionAttribute, PropertyInfo>();
            var allProperties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in allProperties)
            {
                var attr = property.GetCustomAttributes(typeof(ItemCollectionAttribute), false);
                if (attr != null && attr.Length > 0)
                {
                    ItemCollectionAttribute title = attr.FirstOrDefault() as ItemCollectionAttribute;
                    if (title != null)
                        properties.Add(title, property);
                }
            }
            return (from KeyValuePair<ItemCollectionAttribute, PropertyInfo> item in properties
                    orderby item.Key.Order ascending,
                            item.Key.Region ascending
                    select new { key = item.Key, value = item.Value })
                        .ToDictionary(item => item.key, item => item.value);
        }

        private HtmlTableCell CheckItemCell(string itemId = "")
        {
            HtmlTableCell checkBoxCell = new HtmlTableCell();
            HtmlInputCheckBox checkBox = new HtmlInputCheckBox();
            checkBox.Attributes["class"] = "check-item";
            checkBox.Attributes["itemId"] = itemId;
            checkBoxCell.Attributes["style"] = "width: 40px;";
            checkBoxCell.Controls.Add(checkBox);
            return checkBoxCell;
        }

        private void CreateHeader(HtmlTable tblProviders, Dictionary<ItemCollectionAttribute, PropertyInfo> properties, bool showCheckers)
        {
            List<HtmlTableCell> headerCells = new List<HtmlTableCell>();

            // Checkbox for all items, presented in the table
            if (showCheckers) headerCells.Add(CheckItemCell());

            foreach (var itemHead in properties.Keys)
            {
                if (itemHead.IsId && !showCheckers)
                    continue;
                LayoutFactory.DrawHeader(itemHead, headerCells);
            }

            HtmlTableRow headRow = new HtmlTableRow();
            headRow.Attributes["class"] = "header";

            headerCells.ForEach(cell => headRow.Cells.Add(cell));
            tblProviders.Rows.Add(headRow);
        }

        private void CreateAdvancedHeader(HtmlTable tblProviders, Dictionary<ItemCollectionAttribute, PropertyInfo> properties, bool showCheckers)
        {
            List<HtmlTableCell> headerCells = new List<HtmlTableCell>();

            // Checkbox for all items, presented in the table
            if (showCheckers) headerCells.Add(CheckItemCell());

            HtmlTableRow headRow = new HtmlTableRow();
            headRow.Attributes["class"] = "header";

            // Search cell
            HtmlTableCell searchCell = new HtmlTableCell();
            HtmlInputText searchInput = new HtmlInputText();
            searchInput.Attributes["name"] = "WidgetFilter";
            searchInput.Attributes["data-placeholder"] = FilterDefaultText;
            searchInput.Attributes["class"] = "plugin-filter";
            searchInput.Attributes["autocomplete"] = "off";
            searchCell.Controls.Add(searchInput);
            headerCells.Add(searchCell);

            // Add/Delete buttons
            HtmlTableCell controlsCell = new HtmlTableCell();
            foreach (var button in HeaderButtons())
            {
                controlsCell.Controls.Add(button);
            }

            controlsCell.Attributes["class"] = "header-controls";

            headerCells.Add(controlsCell);

            headerCells.ForEach(cell => headRow.Cells.Add(cell));
            tblProviders.Rows.Add(headRow);
        }

        private void ProviderDataBind(HtmlTable tblProviders, object item, Dictionary<ItemCollectionAttribute, PropertyInfo> properties, bool showCheckers)
        {
            List<HtmlTableCell> rowCells = new List<HtmlTableCell>();
            foreach (var attr in properties.Keys)
            {
                object obj = properties[attr].GetValue(item, null);
                string value = HttpContext.Current.Server.HtmlEncode(obj != null ? obj.ToString() : String.Empty);
                if (attr.IsId)
                {
                    if (showCheckers)
                    {
                        // Checkbox for every item, presented in the table
                        rowCells.Add(CheckItemCell(value));
                    }
                }
                else
                {
                    LayoutFactory.DrawContent(value, attr, rowCells);
                }
            }
            HtmlTableRow tblRow = new HtmlTableRow();
            rowCells.ForEach(cell => tblRow.Cells.Add(cell));
            tblProviders.Rows.Add(tblRow);
        }
        #endregion
    }
}