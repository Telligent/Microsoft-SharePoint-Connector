using System;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensions.OpenSearch.Controls.Layout;

namespace Telligent.Evolution.Extensions.OpenSearch.Controls
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ItemCollectionAttribute : Attribute
    {
        private static int NextId = 1;
        private int id;

        private string resource_key;
        private string resource_path;

        string text = String.Empty;
        public string Text
        {
            get
            {
                if (!String.IsNullOrEmpty(text))
                    return text;
                return !String.IsNullOrEmpty(resource_path) ? ResourceManager.GetString(resource_key, resource_path) : String.Empty;
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
            this.IsId = false;
            this.Region = Region.Title;
            this.Order = 0;
            this.Filtered = false;
            this.id = NextId++;
        }

        public ItemCollectionAttribute(bool isId)
        {
            // default values
            this.IsId = IsId;
        }

        public ItemCollectionAttribute(string resource_key, string resource_path)
            : this()
        {
            this.resource_key = resource_key;
            this.resource_path = resource_path;
        }
    }
}
