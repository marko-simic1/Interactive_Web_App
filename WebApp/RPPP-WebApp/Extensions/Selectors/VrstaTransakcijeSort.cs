using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class VrstaTransakcijeSort
    {
        public static IQueryable<VrstaTransakcije> ApplySort(this IQueryable<VrstaTransakcije> query, int sort, bool ascending)
        {
            Expression<Func<VrstaTransakcije, object>> orderSelector = sort switch
            {
                1 => v => v.IdVrste,
                2 => v => v.ImeVrste,
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