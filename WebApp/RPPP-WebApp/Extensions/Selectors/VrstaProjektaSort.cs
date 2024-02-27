using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class VrstaProjektaSort
    {
        public static IQueryable<VrstaProjekta> ApplySort(this IQueryable<VrstaProjekta> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<VrstaProjekta, object>> orderSelector = null;
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
