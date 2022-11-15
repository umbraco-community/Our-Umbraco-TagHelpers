namespace Our.Umbraco.TagHelpers.Models
{
    public class SelfHostedFile
    {
        public string? ExternalUrl { get; internal set; }
        public string? FileName { get; internal set; }
        public string? FolderPath { get; internal set; }
        public string? Url { get; internal set; }

        public override string? ToString()
        {
            return Url;
        }
    }
}
