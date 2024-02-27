using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class VrstaDokumentacijeSort
    {
        public static IQueryable<VrstaDok> ApplySort(this IQueryable<VrstaDok> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<VrstaDok, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = d => d.ImeVrste;
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
