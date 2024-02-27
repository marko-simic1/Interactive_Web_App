using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class UlogaSort
    {
        public static IQueryable<Uloga> ApplySort(this IQueryable<Uloga> query, int sort, bool ascending)
        {
            Expression<Func<Uloga, object>> orderSelector = sort switch
            {
                1 => d => d.IdUloge,
                2 => d => d.ImeUloge,
                3 => d => d.Opis,
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
