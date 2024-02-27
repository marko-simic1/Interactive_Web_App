using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class TransakcijaSort
    {
        public static IQueryable<Transakcija> ApplySort(this IQueryable<Transakcija> query, int sort, bool ascending)
        {
            Expression<Func<Transakcija, object>> orderSelector = sort switch
            {
                1 => t => t.IdTrans,
                2 => t => t.Model,
                3 => t => t.PozivNaBr,
                4 => t => t.IdVrsteNavigation.ImeVrste,
                5 => t => t.Iznos,
                6 => t => t.Datum,
                7 => t => t.Opis,
                8 => t => t.BrKartice,
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