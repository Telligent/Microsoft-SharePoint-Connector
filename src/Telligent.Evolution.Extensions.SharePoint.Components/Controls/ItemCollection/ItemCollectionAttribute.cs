using System;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ItemCollectionAttribute : Attribute
    {
        private static int NextId = 1;

        private int id;

        string text = String.Empty;
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }
        public string CssClass { get; set; }
        public string Style { get; set; }
        /// <summary>
        /// The layout region
        /// </summary>
        public Region Region { get; set; }
        /// <summary>
        /// The column order in the table
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Attribute tells to use a javascript filter
        /// </summary>
        public bool Filtered { get; set; }
        /// <summary>
        /// Property tells if this is an Id
        /// </summary>
        public bool IsId { get; set; }

        public ItemCollectionAttribute()
        {
            // default values
            IsId = false;
            Region = Region.Title;
            Order = 0;
            Filtered = false;
            id = NextId++;
        }

        public ItemCollectionAttribute(bool isId)
        {
            // default values
            IsId = isId;
            id = NextId++;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
