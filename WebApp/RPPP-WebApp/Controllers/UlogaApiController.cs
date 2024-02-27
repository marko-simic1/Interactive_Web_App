using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using RPPP_WebApp.Controllers;
using RPPP_WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using RPPP_WebApp.Models;
using WebServices.Util.ExceptionFilters;
using RPPP_WebApp.ViewModels;
using NLog.Fluent;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Web API kontroler za upravljanje ulogama.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [TypeFilter(typeof(ProblemDetailsForSqlException))]
    public class UlogaApiController : ControllerBase, ICustomController<int, UlogeViewModel>
    {
        private readonly ProjektDbContext ctx;

        /// <summary>
        /// Rječnik za mapiranje naziva stupaca na odgovarajuće Lambda izraze za sortiranje.
        /// </summary>
        private static Dictionary<string, Expression<Func<Uloga, object>>> orderSelectors = new()
        {
            [nameof(UlogeViewModel.IdUloge).ToLower()] = m => m.IdUloge,
            [nameof(UlogeViewModel.ImeUloge).ToLower()] = m => m.ImeUloge,
            [nameof(UlogeViewModel.Opis).ToLower()] = m => m.Opis,
            [nameof(UlogeViewModel.IdVrste).ToLower()] = m => m.IdVrste
        };

        /// <summary>
        /// Lambda izraz za projekciju modela uloge na ViewModel uloge.
        /// </summary>
        private static Expression<Func<Uloga, UlogeViewModel>> projection = m => new UlogeViewModel
        {
            IdUloge = m.IdUloge,
            ImeUloge = m.ImeUloge,
            Opis = m.Opis,
            IdVrste = m.IdVrste
        };

        /// <summary>
        /// Konstruktor UlogaApiController-a.
        /// </summary>
        /// <param name="ctx">Kontekst baze podataka.</param>
        public UlogaApiController(ProjektDbContext ctx)
        {
            this.ctx = ctx;
        }

        /// <summary>
        /// Dohvaća broj uloga prema zadanim kriterijima filtra.
        /// </summary>
        /// <param name="filter">Filter za pretraživanje imena uloga.</param>
        /// <returns>Broj uloga koji zadovoljavaju kriterije filtriranja.</returns>
        [HttpGet("count", Name = "BrojUloga")]
        public async Task<int> Count([FromQuery] string filter)
        {
            var query = ctx.Uloga.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(m => m.ImeUloge.Contains(filter));
            }
            int count = await query.CountAsync();
            return count;
        }

        /// <summary>
        /// Dohvaća sve uloge prema zadanim parametrima.
        /// </summary>
        /// <param name="loadParams">Parametri za učitavanje podataka (filtriranje, sortiranje, paginacija).</param>
        /// <returns>Lista ViewModela uloga koje zadovoljavaju zadane parametre.</returns>
        [HttpGet(Name = "DohvatiMjesta")]
        public async Task<List<UlogeViewModel>> GetAll([FromQuery] LoadParams loadParams)
        {
            var query = ctx.Uloga.AsQueryable();

            if (!string.IsNullOrWhiteSpace(loadParams.Filter))
            {
                query = query.Where(m => m.ImeUloge.Contains(loadParams.Filter));
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
        /// Dohvaća informacije o određenoj ulozi prema njenom identifikatoru.
        /// </summary>
        /// <param name="id">Identifikator uloge.</param>
        /// <returns>ActionResult koji sadrži ViewModel tražene uloge ili NotFound ako uloga ne postoji.</returns>
        [HttpGet("{id}", Name = "DohvatiMjesto")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UlogeViewModel>> Get(int id)
        {
            var uloga = await ctx.Uloga
                                  .Where(m => m.IdUloge == id)
                                  .Select(projection)
                                  .FirstOrDefaultAsync();
            if (uloga == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
            }
            else
            {
                return uloga;
            }
        }

        /// <summary>
        /// Briše ulogu prema njenom identifikatoru.
        /// </summary>
        /// <param name="id">Identifikator uloge za brisanje.</param>
        /// <returns>ActionResult koji sadrži informacije o rezultatu brisanja.</returns>
        [HttpDelete("{id}", Name = "ObrisiUloga")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var uloga = await ctx.Uloga.FindAsync(id);
            if (uloga == null)
            {
                return NotFound();
            }
            else
            {
                ctx.Remove(uloga);
                await ctx.SaveChangesAsync();
                return NoContent();
            };
        }

        /// <summary>
        /// Ažurira podatke o ulozi prema zadanim parametrima.
        /// </summary>
        /// <param name="id">Identifikator uloge koju treba ažurirati.</param>
        /// <param name="model">ViewModel s novim podacima za ulogu.</param>
        /// <returns>ActionResult koji sadrži informacije o rezultatu ažuriranja.</returns>
        [HttpPut("{id}", Name = "AzurirajMjesto")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, UlogeViewModel model)
        {
            if (model.IdUloge != id)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids {id} vs {model.IdUloge}");
            }
            else
            {
                var uloga = await ctx.Uloga.FindAsync(id);
                if (uloga == null)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
                }

                uloga.ImeUloge = model.ImeUloge;
                uloga.IdUloge = model.IdUloge;
                uloga.Opis = model.Opis;
                uloga.IdVrste = model.IdVrste;

                await ctx.SaveChangesAsync();
                return NoContent();
            }
        }

        /// <summary>
        /// Dodaje novu ulogu prema zadanim podacima.
        /// </summary>
        /// <param name="model">ViewModel s podacima za novu ulogu.</param>
        /// <returns>ActionResult koji sadrži informacije o rezultatu dodavanja.</returns>
        [HttpPost(Name = "DodajMjesto")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(UlogeViewModel model)
        {
            Uloga uloga = new Uloga
            {
                ImeUloge = model.ImeUloge,
                IdUloge = model.IdUloge,
                Opis = model.Opis,
                IdVrste = model.IdVrste
            };
            ctx.Add(uloga);
            await ctx.SaveChangesAsync();

            var addedItem = await Get(uloga.IdUloge);

            return CreatedAtAction(nameof(Get), new { id = uloga.IdUloge }, addedItem.Value);
        }
    }
}
