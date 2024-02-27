﻿using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class PosaoSort
    {
        public static IQueryable<Posao> ApplySort(this IQueryable<Posao> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Posao, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = d => d.ImePosla;
                    break;
                case 2:
                    orderSelector = d => d.Opis;
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