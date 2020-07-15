using Fulgoribus.Luxae.Dapper.Identity;
using Fulgoribus.Luxae.Dapper.Repositories;
using Fulgoribus.Luxae.Repositories;
using Lamar;
using Microsoft.AspNetCore.Identity;

namespace Fulgoribus.Luxae.Dapper
{
    public class DapperServiceRegistry : ServiceRegistry
    {
        public DapperServiceRegistry()
        {
            For<IBookRepository>().Use<BookRepository>().Scoped();
            For<IUserStore<IdentityUser>>().Use<DapperUserStore>().Scoped();
        }
    }
}
