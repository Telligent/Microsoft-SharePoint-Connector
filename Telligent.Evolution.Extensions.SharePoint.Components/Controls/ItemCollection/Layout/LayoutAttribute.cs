using System;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout
{
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class LayoutAttribute : Attribute
    {
        public LayoutFactory Layout { get; private set; }

        public LayoutAttribute(Type layoutType)
        {
            if (layoutType.BaseType == typeof(LayoutFactory) && !layoutType.IsAbstract)
            {
                this.Layout = (LayoutFactory)Activator.CreateInstance(layoutType);
            }
        }
    }
}
