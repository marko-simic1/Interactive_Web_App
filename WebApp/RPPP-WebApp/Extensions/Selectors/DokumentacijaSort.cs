using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class DokumentacijaSort
    {
        public static IQueryable<Dokumentacija> ApplySort(this IQueryable<Dokumentacija> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Dokumentacija, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = d => d.ImeDok;
                    break;
                case 2:
                    orderSelector = d => d.IdProjektaNavigation.ImeProjekta;
                    break;
                case 3:
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
