using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class VrstaUlogeSort
    {
        public static IQueryable<VrstaUloge> ApplySort(this IQueryable<VrstaUloge> query, int sort, bool ascending)
        {
            Expression<Func<VrstaUloge, object>> orderSelector = sort switch
            {
                1 => d => d.IdVrste,
                2 => d => d.ImeVrste,
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
