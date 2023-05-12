namespace Our.Umbraco.TagHelpers.Models
{
    public class SelfHostedFile
    {
        public string? ExternalUrl { get; set; }
        public string? FileName { get; set; }
        public string? FolderPath { get; set; }
        public string? Url { get; set; }

        public override string? ToString()
        {
            return Url;
        }
    }
}
