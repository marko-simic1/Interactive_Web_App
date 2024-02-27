using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RPPP_WebApp.Models;
using RPPP_WebApp.Util.ExceptionFilters;
using RPPP_WebApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Web API servis za rad s mjestima
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [TypeFilter(typeof(ProblemDetailsForSqlException))]
    public class JZadatakController : ControllerBase, ICustomController<int, JZadatakViewModel>
    {
        private readonly ProjektDbContext ctx;
        private static Dictionary<string, Expression<Func<Zadatak, object>>> orderSelectors = new()
        {
            [nameof(JZadatakViewModel.IdZadatka).ToLower()] = m => m.IdZadatka,
            [nameof(JZadatakViewModel.Naslov).ToLower()] = m => m.Naslov,
            //[nameof(JZadatakViewModel.IdZahtjevaNavigation).ToLower()] = m => m.IdZahtjevaNavigation,
            [nameof(JZadatakViewModel.PlanPocetak).ToLower()] = m => m.PlanPocetak,
            [nameof(JZadatakViewModel.PlanKraj).ToLower()] = m => m.PlanKraj,
            [nameof(JZadatakViewModel.Prioritet).ToLower()] = m => m.Prioritet
        };

        private static Expression<Func<Zadatak, JZadatakViewModel>> projection = z => new JZadatakViewModel
        {
            IdZadatka = z.IdZadatka,
            Naslov = z.Naslov,
            //IdZahtjevaNavigation = z.IdZahtjevaNavigation,
            PlanPocetak = z.PlanPocetak,
            PlanKraj = z.PlanKraj,
            Prioritet = z.Prioritet
        };

        public JZadatakController(ProjektDbContext ctx)
        {
            this.ctx = ctx;
        }


        /// <summary>
        /// Vraća broj svih mjesta filtriran prema nazivu mjesta 
        /// </summary>
        /// <param name="filter">Opcionalni filter za naziv mjesta</param>
        /// <returns></returns>
        [HttpGet("count", Name = "BrojZadataka")]
        public async Task<int> Count([FromQuery] string filter)
        {
            var query = ctx.Zadatak.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(m => m.Naslov.Contains(filter));
            }
            int count = await query.CountAsync();
            return count;
        }

        /// <summary>
        /// Dohvat mjesta (opcionalno filtrirano po nazivu mjesta).
        /// Broj mjesta, poredak, početna pozicija određeni s loadParams.
        /// </summary>
        /// <param name="loadParams">Postavke za straničenje i filter</param>
        /// <returns></returns>
        [HttpGet(Name = "DohvatiZadatke")]
        public async Task<List<JZadatakViewModel>> GetAll([FromQuery] LoadParams loadParams)
        {
            var query = ctx.Zadatak.AsQueryable();

            if (!string.IsNullOrWhiteSpace(loadParams.Filter))
            {
                query = query.Where(m => m.Naslov.Contains(loadParams.Filter));
            }

            if (loadParams.SortColumn != null)
            {
                if (orderSelectors.TryGetValue(loadParams.SortColumn.ToLower(), out var expr))
                {
                    query = loadParams.Descending ? query.OrderByDescending(expr) : query.OrderBy(expr);
                }
            }

            var list = await query.Select(projection)
                                  .Skip(loadParams.StartIndex)
                                  .Take(loadParams.Rows)
                                  .ToListAsync();
            return list;
        }

        /// <summary>
        /// Vraća grad čiji je IdMjesta jednak vrijednosti parametra id
        /// </summary>
        /// <param name="id">IdMjesta</param>
        /// <returns></returns>
        [HttpGet("{id}", Name = "DohvatiZadatak")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JZadatakViewModel>> Get(int id)
        {
            var zadatak = await ctx.Zadatak
                                  .Where(m => m.IdZadatka == id)
                                  .Select(projection)
                                  .FirstOrDefaultAsync();
            if (zadatak == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
            }
            else
            {
                return zadatak;
            }
        }


        /// <summary>
        /// Brisanje mjesta određenog s id
        /// </summary>
        /// <param name="id">Vrijednost primarnog ključa (Id mjesta)</param>
        /// <returns></returns>
        /// <response code="204">Ako je mjesto uspješno obrisano</response>
        /// <response code="404">Ako mjesto s poslanim id-om ne postoji</response>      
        [HttpDelete("{id}", Name = "ObrisiZadatak")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var zadatak = await ctx.Zadatak.FindAsync(id);
            if (zadatak == null)
            {
                return NotFound();
            }
            else
            {
                ctx.Remove(zadatak);
                await ctx.SaveChangesAsync();
                return NoContent();
            };
        }

        /// <summary>
        /// Ažurira mjesto
        /// </summary>
        /// <param name="id">parametar čija vrijednost jednoznačno identificira mjesto</param>
        /// <param name="model">Podaci o mjestu. IdMjesta mora se podudarati s parametrom id</param>
        /// <returns></returns>
        [HttpPut("{id}", Name = "AzurirajZadatak")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, JZadatakViewModel model)
        {
            if (model.IdZadatka != id) //ModelState.IsValid i model != null provjera se automatski zbog [ApiController]
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids {id} vs {model.IdZadatka}");
            }
            else
            {
                var zadatak = await ctx.Zadatak.FindAsync(id);
                if (zadatak == null)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
                }

                zadatak.Naslov = model.Naslov;
                zadatak.PlanPocetak = model.PlanPocetak;
                zadatak.PlanKraj = model.PlanKraj;
                zadatak.Prioritet = model.Prioritet;

                await ctx.SaveChangesAsync();
                return NoContent();
            }
        }

        /// <summary>
        /// Stvara novo mjesto opisom poslanim modelom
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost(Name = "DodajZadatak")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(JZadatakViewModel model)
        {
            Zadatak zadatak = new Zadatak
            {
                Naslov = model.Naslov,
                PlanPocetak = model.PlanPocetak,
                PlanKraj = model.PlanKraj,
                Prioritet = model.Prioritet
            };
            ctx.Add(zadatak);
            await ctx.SaveChangesAsync();

            var addedItem = await Get(zadatak.IdZadatka);

            return CreatedAtAction(nameof(Get), new { id = zadatak.IdZadatka }, addedItem.Value);
        }
    }
}
