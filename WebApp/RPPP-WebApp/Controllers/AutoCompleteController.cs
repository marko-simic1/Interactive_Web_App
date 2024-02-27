using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    public class AutoCompleteController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly AppSettings appSettings;
        public AutoCompleteController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options)
        {
            this._context = ctx;
            appSettings = options.Value;
        }


        public async Task<IEnumerable<IdLabel>> Partner(string term)
        {
            var query = _context.Partner
                            .Select(m => new IdLabel
                            {
                                Id = m.IdPartnera,
                                Label = m.Ime + " (" + m.Oib + ")"
                            })
                            .Where(l => l.Label.Contains(term));

            var list = await query.OrderBy(l => l.Label)
                                  .ThenBy(l => l.Id)
                                  .Take(appSettings.AutoCompleteCount)
                                  .ToListAsync();
            return list;
        }

        public async Task<IEnumerable<IdLabel>> Uloga(string term)
        {
            var query = _context.Uloga
                            .Select(m => new IdLabel
                            {
                                Id = m.IdUloge,
                                Label = m.ImeUloge
                            })
                            .Where(l => l.Label.Contains(term));

            var list = await query.OrderBy(l => l.Label)
                                  .ThenBy(l => l.Id)
                                  .Take(appSettings.AutoCompleteCount)
                                  .ToListAsync();
            return list;
        }


    }


}
