namespace Fulgoribus.Luxae.Web
{
    public class TwoFactorAuthenticationOptions
    {
        internal const string SectionName = "TwoFactorAuthentication";

        public string Issuer { get; set; } = string.Empty;

        public string AuthenticatorUriFormat { get; set; } = string.Empty;
    }
}
