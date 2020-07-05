namespace Fulgoribus.Luxae.Web
{
    public class SendGridOptions
    {
        internal const string SectionName = "SendGrid";

        public string ApiKey { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
    }
}
