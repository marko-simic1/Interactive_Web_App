using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using SkiaSharp;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Controller za obavljanje operacija nad projektima i za prikaz Razor View-a vezanih uz dokumente.
    /// </summary>
    public class ProjektController : Controller
    {

        private readonly ProjektDbContext ctx;
        private readonly AppSettings appSettings;
        private readonly ILogger<ProjektController> logger;


        /// <summary>
        /// Konstruktor razreda ProjektController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>
        public ProjektController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<ProjektController> logger)
        {
            this.ctx = ctx;
            this.appSettings = options.Value;
            this.logger = logger;
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih projekata u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.Projekt.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedan projekt");
                TempData[Constants.Message] = "Ne postoji niti jedan projekt.";
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Create));
            }

            var pagingInfo = new PagingInfo
            {
                CurrentPage = page,
                Sort = sort,
                Ascending = ascending,
                ItemsPerPage = pagesize,
                TotalItems = count
            };
            if (page < 1 || page > pagingInfo.TotalPages)
            {
                return RedirectToAction(nameof(Index), new { page = 1, sort, ascending });
            }

            query = query.ApplySort(sort, ascending);

            var projekti = await query
                          .Select(i => new ProjektViewModel
                          {
                              IdProjekta = i.IdProjekta,
                              ImeProjekta = i.ImeProjekta,
                              Kratica = i.Kratica,
                              Sazetak = i.Sazetak,
                              DatumPoc = i.DatumPoc,
                              DatumZav = i.DatumZav,
                              BrKartice = i.BrKartice,
                              imeVrste = i.IdVrsteNavigation.ImeVrste
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new ProjektiViewModel
            {
                Projekti = projekti,
                PagingInfo = pagingInfo
            };

            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;
            await PrepareDropDownLists();

            return View(model);
        }

        /// <summary>
        /// End point koji vraća formu za kreiranje projekta.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje projekta. </returns>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PrepareDropDownLists();
            return View();
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem novog projekta.
        /// </summary>
        /// <param name="projekt">ViewModel u koji unosimo projekt.</param>
        /// <returns>Redirect na početnu stranicu projekta. </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Projekt projekt)
        {
            logger.LogTrace(JsonSerializer.Serialize(projekt));
            if (ModelState.IsValid)
            {
                try
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt dodan - Id: {projekt.IdProjekta}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    // Create a new Kartica object (without setting BrKartice explicitly)
                    Kartica kartica = new Kartica();

                    // Set the BrKarticeNavigation property of the projekt to the created Kartica object
                    projekt.BrKarticeNavigation = kartica;

                    if (projekt.DatumZav < projekt.DatumPoc)
                    {
                        ModelState.AddModelError(nameof(Projekt.DatumZav), "Datum završetka ne može biti prije datuma početka.");
                        await PrepareDropDownLists();
                        return View(projekt);
                    }

                    // Add both objects to the context
                    ctx.Add(projekt);
                    ctx.Add(kartica);

                    // Save changes to the database
                    await ctx.SaveChangesAsync();

                    TempData[Constants.Message] = $"Projekt dodan. Id projekta = {projekt.IdProjekta}";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    await PrepareDropDownLists();
                    return View(projekt);
                }
            }
            else
            {
                await PrepareDropDownLists();
                return View(projekt);
            }
        }

        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz projekt.
        /// </summary>
        /// <param name="id">Id projekta koji se traži.</param>
        /// <returns>vraća prikaz sadržaja projekta. </returns>
        /// <response code="404">Not found ukoliko ne postoji projekt s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var proj = await ctx.Projekt
                                  .Where(m => m.IdProjekta == id)
                                  .Include(d => d.IdVrsteNavigation)
                                  .Select(m => new ProjektViewModel
                                  {
                                      IdProjekta = m.IdProjekta,
                                      ImeProjekta = m.ImeProjekta,
                                      imeVrste = m.IdVrsteNavigation.ImeVrste,
                                      IdVrste = m.IdVrste,
                                      Kratica = m.Kratica,
                                      Sazetak = m.Sazetak,
                                      BrKartice = m.BrKartice,
                                      DatumPoc = m.DatumPoc,
                                      DatumZav = m.DatumZav

                                  })
                                  .SingleOrDefaultAsync();
            if (proj != null)
            {
                return PartialView(proj);
            }
            else
            {
                return NotFound($"Neispravan id projekta: {id}");
            }
        }


        /// <summary>
        /// Metoda za pripremu padajućih lista
        /// </summary>
        private async Task PrepareDropDownLists()
        {

            var vrstaProj = await ctx.VrstaProjekta.OrderBy(d => d.ImeVrste)
                             .Select(d => new { d.ImeVrste, d.IdVrste })
                             .ToListAsync();

            ViewBag.VrsteProjekata = new SelectList(vrstaProj, nameof(VrstaProjekta.IdVrste), nameof(VrstaProjekta.ImeVrste));
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem projekta.
        /// </summary>
        /// <param name="id">Id projekta kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await ctx.Projekt
                .Include(d => d.IdVrsteNavigation)
                .FirstOrDefaultAsync(d => d.IdProjekta == id);

            if (vrsta != null)
            {
                try
                {
                    ctx.Remove(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt obrisan - Id: {id}, Ime: {vrsta.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Projekt {vrsta.ImeProjekta} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"Projekt {vrsta.ImeProjekta} sa šifrom {id} uspješno obrisan.");
                    TempData[Constants.Message] = $"Projekt {vrsta.ImeProjekta} uspješno obrisan.";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja projekta: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja projekta: " + exc.CompleteExceptionMessage());
                    TempData[Constants.Message] = "Pogreška prilikom brisanja projekta";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Projekt sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
            }
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem projekta u MD prikazu.
        /// </summary>
        /// <param name="id">Id projekta kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        [HttpDelete]
        public async Task<IActionResult> Delete2(int id)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await ctx.Projekt
                .Include(d => d.IdVrsteNavigation)
                .FirstOrDefaultAsync(d => d.IdProjekta == id);

            if (vrsta != null)
            {
                try
                {
                    ctx.Remove(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt obrisan - Id: {id}, Ime: {vrsta.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Projekt {vrsta.ImeProjekta} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"Projekt {vrsta.ImeProjekta} sa šifrom {id} uspješno obrisan.");
                    TempData[Constants.Message] = $"Projekt {vrsta.ImeProjekta} uspješno obrisan.";
                    TempData[Constants.ErrorOccurred] = false;
                    var projekti2 = await ctx.Projekt.OrderBy(z => z.IdProjekta).ToListAsync();
                    int newid2 = projekti2.First().IdProjekta;
                    Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
                    return RedirectToAction(nameof(Index2), new { id = newid2 });

                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja projekta: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja projekta: " + exc.CompleteExceptionMessage());
                    TempData[Constants.Message] = "Pogreška prilikom brisanja projekta";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Projekt sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
            }
            var projekti = await ctx.Projekt.OrderBy(z => z.IdProjekta).ToListAsync();
            int newid = projekti.First().IdProjekta;
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return RedirectToAction(nameof(Index2), new { id = newid });
        }

        /// <summary>
        /// End point na koje se šalje id za formom za ažuriranje projekta.
        /// </summary>
        /// <param name="id">Id projekta kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima dokumentacije.</returns>
        /// <response code="404">Not found ukoliko ne postoji projekt s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var projekt = await ctx.Projekt
                                   .Include(d => d.IdVrsteNavigation)     // Include the related VrstaDok entity
                                  .AsNoTracking()
                                  .Where(i => i.IdProjekta == id)
                                  .SingleOrDefaultAsync();
            if (projekt != null)
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                await PrepareDropDownLists();
                return PartialView(projekt);
            }
            else
            {
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
                return NotFound("Ne postoji projekt s oznakom: " + id);
            }
        }

        /// <summary>
        /// End point na koje se šalje id za formom za ažuriranje projekta u MD prikazu.
        /// </summary>
        /// <param name="id">Id projekta kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima dokumentacije.</returns>
        /// <response code="404">Not found ukoliko ne postoji projekt s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Edit2(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var projekt = await ctx.Projekt
                                   .Include(d => d.IdVrsteNavigation)     // Include the related VrstaDok entity
                                  .AsNoTracking()
                                  .Where(i => i.IdProjekta == id)
                                  .SingleOrDefaultAsync();
            if (projekt != null)
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                await PrepareDropDownLists();
                return PartialView(projekt);
            }
            else
            {
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
                return NotFound("Ne postoji projekt s oznakom: " + id);
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje projekta.
        /// </summary>
        /// <param name="id">Id projekta koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji projekt s tim id-om.</response>
        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> Editing(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            Projekt projekt = await ctx.Projekt
                                  .Where(d => d.IdProjekta == id)
                                  .FirstOrDefaultAsync();
            if (projekt == null)
            {
                return NotFound();
            }
            bool checkId = await ctx.Projekt.AnyAsync(i => i.IdProjekta == projekt.IdProjekta);
            if (!checkId)
            {
                return NotFound($"Neispravan id projekta: {projekt?.IdProjekta}");
            }

            if (await TryUpdateModelAsync<Projekt>(projekt, "",
                    i => i.ImeProjekta, i => i.Kratica, i => i.Sazetak, i => i.DatumPoc, i => i.DatumZav, i => i.BrKartice, i => i.IdVrste))
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;

                if (projekt.DatumZav < projekt.DatumPoc)
                {
                    ModelState.AddModelError(nameof(Projekt.DatumZav), "Datum završetka ne može biti prije datuma početka.");
                    await PrepareDropDownLists();
                    return View(projekt);
                }
                try
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt azuriran - Id: {projekt.IdProjekta}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = "Projekt ažuriran.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Get), new { id = projekt.IdProjekta });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(projekt);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Podatke o projektu nije moguće povezati s forme");
                return View(projekt);
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje projekta u MD prikazu.
        /// </summary>
        /// <param name="id">Id projekta koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji projekt s tim id-om.</response>
        [HttpPost, ActionName("Edit2")]
        public async Task<IActionResult> Editing2(int id)
        {
            Projekt projekt = await ctx.Projekt
                                  .Where(d => d.IdProjekta == id)
                                  .FirstOrDefaultAsync();
            if (projekt == null)
            {
                return NotFound();
            }
            bool checkId = await ctx.Projekt.AnyAsync(i => i.IdProjekta == projekt.IdProjekta);
            if (!checkId)
            {
                return NotFound($"Neispravan id projekta: {projekt?.IdProjekta}");
            }

            if (await TryUpdateModelAsync<Projekt>(projekt, "",
                    i => i.ImeProjekta, i => i.Kratica, i => i.Sazetak, i => i.DatumPoc, i => i.DatumZav, i => i.BrKartice, i => i.IdVrste))
            {


                if (projekt.DatumZav < projekt.DatumPoc)
                {
                    ModelState.AddModelError(nameof(Projekt.DatumZav), "Datum završetka ne može biti prije datuma početka.");
                    await PrepareDropDownLists();
                    return View(projekt);
                }
                try
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt azuriran - Id: {projekt.IdProjekta}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = "Projekt ažuriran.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index2), new { id = projekt.IdProjekta });

                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(projekt);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Podatke o dokumentu nije moguće povezati s forme");
                return View(projekt);
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za MD prikazom svih projekata i njegovih dokumentacija u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        [HttpGet]
        public async Task<IActionResult> Index2(int id)
        {
            try
            {
                var projekt = await ctx.Projekt
                .Where(z => z.IdProjekta == id)
                .Include(z => z.Dokumentacija)
                .ThenInclude(x => x.IdVrsteNavigation)
                .AsNoTracking()
                .SingleOrDefaultAsync();


                if (projekt == null)
                {
                    return NotFound();
                }

                Projekt2ViewModel model = new Projekt2ViewModel(projekt);
                await PrepareDropDownLists();
                return View(model);

            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }

        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail sljedećeg projekta.
        /// </summary>
        /// <param name="crntProjektId">Projekt koji se trenutno prikazuje</param>
        /// <returns>Redirect na master detail projekta </returns>
        /// <resposne code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</resposne>
        /// <resposne code="500">Prilikom greške pri upisivanju u bazu.</resposne>
        [HttpGet]
        public async Task<IActionResult> SljedeciProjekt(int crntProjektId)
        {
            try
            {
                var crntProjekt = await ctx.Projekt
                    .Where(z => z.IdProjekta == crntProjektId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntProjekt == null)
                {
                    return NotFound("Current Projekt not found.");
                }


                var projekti = await ctx.Projekt.OrderBy(z => z.IdProjekta).ToListAsync();

                int index = projekti.FindIndex(z => z.IdProjekta == crntProjektId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int nextOsobaId = projekti[(index + 1) % projekti.Count()].IdProjekta;

                return RedirectToAction("Index2", new { id = nextOsobaId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail prethodnog projekta.
        /// </summary>
        /// <param name="crntProjektId">Projekt koji se trenutno prikatzuje</param>
        /// <returns>Redirect na master detail projekta </returns>
        /// <resposne code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</resposne>
        /// <resposne code="500">Prilikom greške pri upisivanju u bazu.</resposne>
        [HttpGet]
        public async Task<IActionResult> PrethodniProjekt(int crntProjektId)
        {
            try
            {
                var crntProjekt = await ctx.Projekt
                    .Where(z => z.IdProjekta == crntProjektId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntProjekt == null)
                {
                    return NotFound("Current Projekt not found.");
                }


                var projekti = await ctx.Projekt.OrderBy(z => z.IdProjekta).ToListAsync();

                int index = projekti.FindIndex(z => z.IdProjekta == crntProjektId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Projekt.");
                }

                int newIndex = (index - 1) % projekti.Count();

                newIndex = newIndex < 0 ? projekti.Count() - 1 : newIndex;

                int prevProjId = projekti[newIndex].IdProjekta;

                return RedirectToAction("index2", new { id = prevProjId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem vrste uloge putem excela.
        /// </summary>
        /// <param name="file">Datoteka koju pregledavamo.</param>
        /// <returns>Novu datoteku koja označava dodane stavke. </returns>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>
        [HttpPost]
        public async Task<IActionResult> ImportPartnerExcel(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return BadRequest("Invalid file");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];

                        List<Projekt> projList = new List<Projekt>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {

                            string vrstaName = worksheet.Cells[row, 8].Value?.ToString().Trim();
                            VrstaProjekta vrsta = await ctx.VrstaProjekta
                                .Where(v => EF.Functions.Like(v.ImeVrste, vrstaName))
                                .FirstOrDefaultAsync();
                            int parsedBrKartice;
                            bool brKarParsed = int.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out parsedBrKartice);


                            string dateStringPoc = worksheet.Cells[row, 5].Value?.ToString();
                            string dateStringZav = worksheet.Cells[row, 6].Value?.ToString();
                            DateTime parsedDatePoc;
                            DateTime parsedDateZav;

                            // Using DateTime.Parse
                            bool datePocParsed = DateTime.TryParse(dateStringPoc, out parsedDatePoc);
                            bool dateZavParsed = DateTime.TryParse(dateStringZav, out parsedDateZav);

                            var proj = new Projekt
                            {
                                ImeProjekta = worksheet.Cells[row, 1].Value?.ToString(),
                                Kratica = worksheet.Cells[row, 2].Value?.ToString(),
                                Sazetak = worksheet.Cells[row, 4].Value?.ToString(),
                                DatumPoc = parsedDatePoc,
                                DatumZav = parsedDateZav,
                                BrKartice = parsedBrKartice,
                                IdVrste = vrsta.IdVrste
                            };

                            projList.Add(proj);
                        }

                        ctx.AddRange(projList);
                        await ctx.SaveChangesAsync();

                        var newPackage = new ExcelPackage();
                        var newWorksheet = newPackage.Workbook.Worksheets.Add("Sheet1");

                        for (int row = startRow; row <= endRow; row++)
                        {
                            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                            {
                                newWorksheet.Cells[row, col].Value = worksheet.Cells[row, col].Value;
                            }

                            newWorksheet.Cells[row, worksheet.Dimension.End.Column + 1].Value = "dodano";
                        }
                        byte[] newFileBytes = newPackage.GetAsByteArray();
                        string newFileName = "new_file_with_status.xlsx";
                        string newFilePath = Path.Combine(Path.GetTempPath(), newFileName);
                        System.IO.File.WriteAllBytes(newFilePath, newFileBytes);
                        TempData["imported"] = true;
                        return File(newFileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", newFileName);
                    }


                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during data import.");

                TempData["imported"] = false;
                TempData["importError"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}