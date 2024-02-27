using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class ProjektSort
    {
        public static IQueryable<Projekt> ApplySort(this IQueryable<Projekt> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Projekt, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = d => d.ImeProjekta;
                    break;
                case 2:
                    orderSelector = d => d.Kratica;
                    break;
                case 3:
                    orderSelector = d => d.Sazetak;
                    break;
                case 4:
                    orderSelector = d => d.DatumPoc;
                    break;
                case 5:
                    orderSelector = d => d.DatumZav;
                    break;
                case 6:
                    orderSelector = d => d.BrKartice;
                    break;
                case 7:
                    orderSelector = d => d.IdVrsteNavigation.ImeVrste;
                    break;
               
            }

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
