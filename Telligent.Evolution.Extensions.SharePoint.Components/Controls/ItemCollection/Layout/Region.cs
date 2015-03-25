namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout
{
    public enum Region
    {
        [Layout(typeof(TitleLayout))]
        Title,

        [Layout(typeof(SubTitleLayout))]
        SubTitle,

        [Layout(typeof(IconsLayout))]
        Icons,

        [Layout(typeof(HoverButtonsLayout))]
        HoverButtons,

        [Layout(typeof(CustomLayout))]
        Custom
    }
}
