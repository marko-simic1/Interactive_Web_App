using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class OsobaSort
    {
        public static IQueryable<Osoba> ApplySort(this IQueryable<Osoba> query, int sort, bool ascending)
        {
            Expression<Func<Osoba, object>> orderSelector = sort switch
            {
                1 => d => d.Ime,
                2 => d => d.Prezime,
                3 => d => d.IdOsoba,
                4 => d => d.Telefon,
                5 => d => d.Email,
                6 => d => d.Iban,
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
