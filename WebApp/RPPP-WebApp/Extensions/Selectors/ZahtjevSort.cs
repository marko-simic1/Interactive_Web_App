using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class ZahtjevSort
    {
        public static IQueryable<Zahtjev> ApplySort(this IQueryable<Zahtjev> query, int sort, bool ascending)
        {
            Expression<Func<Zahtjev, object>> orderSelector = sort switch
            {
                1 => d => d.Naslov,
                2 => d => d.Opis,
                3 => d => d.IdProjektaNavigation.ImeProjekta,
                4 => d => d.IdVrsteNavigation.ImeZahtjeva,
                5 => d => d.Zadatak.Count,
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

