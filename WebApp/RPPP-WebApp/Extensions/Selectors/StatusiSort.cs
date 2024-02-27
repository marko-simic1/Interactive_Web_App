using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class StatusiSort
    {
        public static IQueryable<Status> ApplySort(this IQueryable<Status> query, int sort, bool ascending)
        {
            Expression<Func<Status, object>> orderSelector = sort switch
            {
                1 => d => d.ImeStatusa,
                2 => d => d.Zadatak.Count(),
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

