using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RPPP_WebApp.Models;
using RPPP_WebApp.Models.JTable;
using System.Linq;
using System.Threading.Tasks;

namespace RPPP_WebApp.Controllers.JTable
{
    /// <summary>
    /// Lookup controller prilagođen za jTable
    /// </summary>
    [ApiController]
    [Route("[controller]/[action]")]
    public class LookupController : ControllerBase
    {
        private readonly ProjektDbContext ctx;

        public LookupController(ProjektDbContext ctx)
        {
            this.ctx = ctx;
        }

        [HttpGet]
        [HttpPost]
        public async Task<OptionsResult> Zahtjev()
        {
            var options = await ctx.Zahtjev.OrderBy(z => z.Naslov)
                                   .Select(z => new TextValue
                                   {
                                       DisplayText = z.Naslov,
                                       Value = z.Naslov
                                   })
                                   .ToListAsync();
            return new OptionsResult(options);
        }
    }
}
