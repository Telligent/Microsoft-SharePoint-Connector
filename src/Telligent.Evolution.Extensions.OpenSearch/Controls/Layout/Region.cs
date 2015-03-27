namespace Telligent.Evolution.Extensions.OpenSearch.Controls.Layout
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
        HoverButtons
    }
}
