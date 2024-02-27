using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class KarticaSort
    {
        public static IQueryable<Kartica> ApplySort(this IQueryable<Kartica> query, int sort, bool ascending)
        {
            Expression<Func<Kartica, object>> orderSelector = sort switch
            {
                1 => k => k.BrKartice,
                2 => k => k.Stanje,
                3 => k => k.Projekt.FirstOrDefault().ImeProjekta,
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
