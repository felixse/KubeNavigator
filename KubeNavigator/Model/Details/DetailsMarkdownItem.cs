namespace KubeNavigator.Model.Details
{
    public class DetailsMarkdownItem : IDetailsItem
    {
        public required string Title { get; set; }

        public required string Value { get; set; }
    }
}
