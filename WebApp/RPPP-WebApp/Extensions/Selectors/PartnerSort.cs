using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class PartnerSort
    {
        public static IQueryable<Partner> ApplySort(this IQueryable<Partner> query, int sort, bool ascending)
        {
            Expression<Func<Partner, object>> orderSelector = sort switch
            {
                1 => d => d.IdPartnera,
                2 => d => d.Ime,
                3 => d => d.Oib,
                4 => d => d.Telefon,
                5 => d => d.Email,
                6 => d => d.Iban,
                7 => d => d.Adresa,
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
