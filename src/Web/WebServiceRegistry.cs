using Fulgoribus.Luxae.Web.Services;
using Lamar;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fulgoribus.Luxae.Web
{
    public class WebServiceRegistry : ServiceRegistry
    {
        public WebServiceRegistry()
        {
            For<IEmailSender>().Use<EmailSender>().Singleton();
        }
    }
}
