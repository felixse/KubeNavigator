namespace KubeNavigator.ViewModels.Details
{
    public class DetailsMarkdownItem : IDetailsItem
    {
        public required string Title { get; set; }

        public required string Value { get; set; }
    }
}
