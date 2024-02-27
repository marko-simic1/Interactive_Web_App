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
using System.Text.Json;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Controller za obavljanje operacija nad dokumentacijom i za prikaz Razor View-a vezanih uz dokumente.
    /// </summary>
    public class DokumentacijaController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly AppSettings appSettings;
        private readonly ILogger<DokumentacijaController> logger;

        /// <summary>
        /// Konstruktor razreda DokumentacijaController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>

        public DokumentacijaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<DokumentacijaController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            this.appSettings = options.Value;
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih dokumentacija u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.Dokumentacija.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedan dokument");
                TempData[Constants.Message] = "Ne postoji niti jedna dokumentacija.";
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

            var dokumentacija = await query
                          .Select(i => new DokumentacijaViewModel
                          {
                              IdDok = i.IdDok,
                              ImeDok = i.ImeDok,
                              imeProjekta = i.IdProjektaNavigation.ImeProjekta,
                              imeVrste = i.IdVrsteNavigation.ImeVrste
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new DokumentacijeViewModel
            {
                Dokumentacija = dokumentacija,
                PagingInfo = pagingInfo
            };

            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;
            await PrepareDropDownLists();

            return View(model);
        }

        /// <summary>
        /// End point koji vraća formu za kreiranje dokumentacije.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje dokumentacije. </returns>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PrepareDropDownLists();
            return View();
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem nove dokumentacije.
        /// </summary>
        /// <param name="vrDok">ViewModel u koji unosimo dokumentaciju.</param>
        /// <returns>Redirect na početnu stranicu dokumentacije. </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Dokumentacija dokument)
        {
            logger.LogTrace(JsonSerializer.Serialize(dokument));
            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Add(dokument);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Dokumentacija dodana - Id: {dokument.IdDok}, Ime: {dokument.ImeDok}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation("Dokukment dodan");
                    TempData[Constants.Message] = $"Dokument dodan. Id dokumenta = {dokument.IdDok}";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje novog dokumenta: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.Message);
                    await PrepareDropDownLists();
                    return View(dokument);
                }
            }
            else
            {
                await PrepareDropDownLists();
                return View(dokument);
            }
        }

        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz dokumentaciju.
        /// </summary>
        /// <param name="id">Id dokumentacije koji se traži.</param>
        /// <returns>vraća prikaz sadržaja dokumentacije. </returns>
        /// <response code="404">Not found ukoliko ne postoji dokumentacija s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var doku = await ctx.Dokumentacija
                                  .Where(m => m.IdDok == id)
                                  .Include(d => d.IdProjektaNavigation)
                                  .Include(d => d.IdVrsteNavigation)
                                  .Select(m => new DokumentacijaViewModel
                                  {
                                      IdDok = m.IdDok,
                                      IdProjekta = m.IdProjekta,
                                      IdVrste = m.IdVrste,
                                      ImeDok = m.ImeDok,
                                      imeProjekta = m.IdProjektaNavigation.ImeProjekta,
                                      imeVrste = m.IdVrsteNavigation.ImeVrste

                                  })
                                  .SingleOrDefaultAsync();
            if (doku != null)
            {
                return PartialView(doku);
            }
            else
            {
                return NotFound($"Neispravan id dokumentacije: {id}");
            }
        }

        /// <summary>
        /// Metoda za pripremu padajućih lista
        /// </summary>
        private async Task PrepareDropDownLists()
        {
            var projekti = await ctx.Projekt.OrderBy(d => d.ImeProjekta)
                             .Select(d => new { d.ImeProjekta, d.IdProjekta })
                             .ToListAsync();


            var vrstaDok = await ctx.VrstaDok.OrderBy(d => d.ImeVrste)
                             .Select(d => new { d.ImeVrste, d.IdVrste })
                             .ToListAsync();

            ViewBag.Projekti = new SelectList(projekti, nameof(Projekt.IdProjekta), nameof(Projekt.ImeProjekta));
            ViewBag.VrsteDokumentacije = new SelectList(vrstaDok, nameof(VrstaDok.IdVrste), nameof(VrstaDok.ImeVrste));
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem dokumentacije.
        /// </summary>
        /// <param name="id">Id dokumentacije kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await ctx.Dokumentacija
                .Include(d => d.IdProjektaNavigation)  // Include the related Projekt entity
                .Include(d => d.IdVrsteNavigation)     // Include the related VrstaDok entity
                .FirstOrDefaultAsync(d => d.IdDok == id);

            if (vrsta != null)
            {
                try
                {
                    ctx.Remove(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Dokumentacija obrisana - Id: {id}, Ime: {vrsta.ImeDok}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Dokumentacija {vrsta.ImeDok} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"Dokumentacija {vrsta.ImeDok} sa šifrom {id} uspješno obrisano.");
                    TempData[Constants.Message] = $"Dokumentacija {vrsta.ImeDok} uspješno obrisana.";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja dokumentacije: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja dokumentacije: " + exc.CompleteExceptionMessage());
                    TempData[Constants.Message] = "Pogreška prilikom brisanja dokumentacije";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Dokumentacija sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji dokumentacija s oznakom: {0} ", id);
            }
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }


        /// <summary>
        /// End point na koje se šalje id za formom za ažuriranje dokumentacije.
        /// </summary>
        /// <param name="id">Id dokumentacije kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima dokumentacije.</returns>
        /// <response code="404">Not found ukoliko ne postoji dokumentacija s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var dokumentacija = await ctx.Dokumentacija
                                   .Include(d => d.IdProjektaNavigation)  // Include the related Projekt entity
                                   .Include(d => d.IdVrsteNavigation)     // Include the related VrstaDok entity
                                  .AsNoTracking()
                                  .Where(i => i.IdDok == id)
                                  .SingleOrDefaultAsync();
            if (dokumentacija != null)
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                await PrepareDropDownLists();
                return PartialView(dokumentacija);
            }
            else
            {
                logger.LogWarning("Ne postoji dokument s oznakom: {0} ", id);
                return NotFound("Ne postoji dokument s oznakom: " + id);
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje dokumentacije.
        /// </summary>
        /// <param name="id">Id dokumentacije koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji dokumentacija s tim id-om.</response>
        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> Editing(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            Dokumentacija dokumentacija = await ctx.Dokumentacija
                                  .Where(d => d.IdDok == id)
                                  .FirstOrDefaultAsync();
            if (dokumentacija == null)
            {
                return NotFound();
            }
            bool checkId = await ctx.Dokumentacija.AnyAsync(i => i.IdDok == dokumentacija.IdDok);
            if (!checkId)
            {
                return NotFound($"Neispravan id dokumenta: {dokumentacija?.IdDok}");
            }

            if (await TryUpdateModelAsync<Dokumentacija>(dokumentacija, "",
                    i => i.ImeDok, i => i.IdProjekta, i => i.IdVrste))
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                try
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Dokumentacija azurirana - Id: {dokumentacija.IdDok}, Ime: {dokumentacija.ImeDok}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = "Dokument ažuriran.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Get), new { id = dokumentacija.IdDok });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(dokumentacija);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Podatke o dokumentu nije moguće povezati s forme");
                return View(dokumentacija);
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

                        List<Dokumentacija> vrsteDokList = new List<Dokumentacija>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            string vrstaName = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            VrstaDok vrsta = await ctx.VrstaDok
                                .Where(v => EF.Functions.Like(v.ImeVrste, vrstaName))
                                .FirstOrDefaultAsync();

                            string projName = worksheet.Cells[row, 3].Value?.ToString().Trim();
                            Projekt projekt = await ctx.Projekt
                                .Where(v => EF.Functions.Like(v.ImeProjekta, projName))
                                .FirstOrDefaultAsync();

                            var doku = new Dokumentacija
                            {
                                ImeDok = worksheet.Cells[row, 1].Value?.ToString(),
                                IdProjekta = projekt.IdProjekta,
                                IdVrste = vrsta.IdVrste
                            };

                            vrsteDokList.Add(doku);
                        }

                        ctx.AddRange(vrsteDokList);
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
