using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class VrsteZahtjevaSort
    {

        public static IQueryable<VrstaZah> ApplySort(this IQueryable<VrstaZah> query, int sort, bool ascending)
        {
            Expression<Func<VrstaZah, object>> orderSelector = sort switch
            {
                1 => d => d.ImeZahtjeva,
                2 => d => d.Zahtjev.Count(),
                _ => null
            };

            if (orderSelector != null)
            {
                query = ascending ?
                        query.OrderBy(orderSelector) :
                        query.OrderByDescending(orderSelector);
            }


            return query;
        }
    }

}

