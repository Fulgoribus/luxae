using Fulgoribus.Luxae.Dapper.Identity;
using Lamar;
using Microsoft.AspNetCore.Identity;

namespace Fulgoribus.Luxae.Dapper
{
    public class DapperServiceRegistry : ServiceRegistry
    {
        public DapperServiceRegistry()
        {
            For<IUserStore<IdentityUser>>().Use<DapperUserStore>().Scoped();
        }
    }
}
