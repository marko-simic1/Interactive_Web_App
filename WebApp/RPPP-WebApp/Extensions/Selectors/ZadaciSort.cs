using RPPP_WebApp.Models;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class ZadaciSort
    {
        public static IQueryable<Zadatak> ApplySort(this IQueryable<Zadatak> query, int sort, bool ascending)
        {
            Expression<Func<Zadatak, object>> orderSelector = sort switch
            {
                1 => d => d.Naslov,
                2 => d => d.Opis,
                3 => d => d.PlanPocetak,
                4 => d => d.PlanKraj,
                5 => d => d.StvPoc,
                6 => d => d.StvKraj,
                7 => d => d.Prioritet,
                8 => d => d.IdZahtjevaNavigation.Naslov,
                9 => d => d.IdStatusaNavigation.ImeStatusa,
                10 => d => d.IdOsobaNavigation.Ime,
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
