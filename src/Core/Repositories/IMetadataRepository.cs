using System.Collections.Generic;
using Fulgoribus.Luxae.Entities;

namespace Fulgoribus.Luxaee.Repositories
{
    public interface IMetadataRepository
    {
        Retailer GetRetailer(string retailerId);
        IEnumerable<Retailer> GetRetailers();
    }
}
