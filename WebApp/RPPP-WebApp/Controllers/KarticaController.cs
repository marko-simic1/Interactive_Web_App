using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using RPPP_WebApp;
using RPPP_WebApp.TagHelpers;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Text.Json;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using Azure;
using Microsoft.Data.SqlClient;
using System.Globalization;
using SkiaSharp;
using OfficeOpenXml;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler za upravljanje operacijama vezanim uz entitet kartice.
    /// </summary>
    public class KarticaController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<KarticaController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor za KarticaController.
        /// </summary>
        /// <param name="ctx">Objekt za pristup bazi podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zapisivanje događaja.</param>
        public KarticaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<KarticaController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// Prikazuje popis kartica s opcijama za straničenje i sortiranje.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s tablicom kartica.</returns>
        public IActionResult Kartice(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.Kartica
                                    .Include(k => k.Projekt)
                                    .Include(k => k.Transakcija)
                                    .AsNoTracking();

            int count = query.Count();

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

            var kartice = query
                .Include(k => k.Projekt)
                .Select(k => new KarticeViewModel
                {
                    BrKartice = k.BrKartice,
                    Stanje = k.Stanje,
                    Projekt = k.Projekt
                })
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            var model = new KarticaViewModel
            {
                kartice = kartice,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Prikazuje stranicu za ažuriranje kartice.
        /// </summary>
        /// <param name="id">Broj kartice</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s formom za ažuriranje kartice</returns>
        [HttpGet]
        public async Task<IActionResult> FormUpdateKartica(int id, int CurrentPage, int Sort, bool Ascending)
        {
            var query = await ctx.Kartica
                .Where(k => k.BrKartice == id)
                .Include(k => k.Projekt)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var Projekti = ctx.Projekt
                .Where(p => p.BrKartice == null || p.BrKartice == id)
                .AsNoTracking().ToList();



            if (query == null)
            {
                // Handle the case where no Zahtjev with the given ID is found
                return NotFound();
            }

            var viewModel = new KarticaViewModel(query, Projekti);
            viewModel.PagingInfo = new PagingInfo { Ascending = Ascending, CurrentPage = CurrentPage, Sort = Sort };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za ažuriranje kartice.
        /// </summary>
        /// <param name="projekt">Atribut projekt entiteta kartice.</param>
        /// <param name="brKartice">Broj kartice.</param>
        /// <param name="stanje">Atribut stanje entiteta kartice.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Vraća redirekt akciju ili HTTP status code.</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateKartica(int projekt, int brKartice, decimal stanje, int page, int sort, bool ascending)
        {

            try
            {
                var existingKartica = await ctx.Kartica.FindAsync(brKartice);

                if (existingKartica == null)
                {
                    return NotFound();
                }

                existingKartica.Stanje = stanje;

                var existingProjekt = await ctx.Projekt.FindAsync(projekt);

                var existingProjekt2 = await ctx.Projekt.Where(p => p.BrKartice == brKartice).FirstOrDefaultAsync();

                if (existingProjekt2 != null)
                {
                    existingProjekt2.BrKartice = null;
                }

                if (existingProjekt != null)
                {
                    existingProjekt.BrKartice = brKartice;
                }

                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kartica uspješno ažurirana.";
                return RedirectToAction("Kartice", new { page = page, sort = sort, ascending = ascending });
            }
            catch (Exception ex)
            {
                // Handle exceptions, log the error, or return an appropriate response
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Prikazuje stranicu za kreiranje kartice.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s formom za kreiranje nove kartice.</returns>
        [HttpGet]
        public IActionResult FormCreateKartica(int page, int sort, bool ascending)
        {
            var Projekti = ctx.Projekt
                                        .Where(p => p.BrKartice == null)
                                        .AsNoTracking().ToList();

            KarticaViewModel viewModel = new KarticaViewModel(new Kartica(), Projekti);

            viewModel.PagingInfo = new PagingInfo { CurrentPage = page, Sort = sort, Ascending = ascending };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za kreiranje nove kartice.
        /// </summary>
        /// <param name="projekt">Atribut projekt entiteta kartice.</param>
        /// <param name="brKartice">Broj kartice.</param>
        /// <param name="stanje">Atribut stanje entiteta kartice.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Vraća redirekt ili HTTP status kod.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateKartica(int projekt, int brKartice, int stanje, int page, int sort, bool ascending)
        {
            try
            {
                Kartica kartica = new Kartica();
                kartica.Stanje = stanje;

                var existingProjekt = await ctx.Projekt.FindAsync(projekt);

                if (existingProjekt != null)
                {
                    existingProjekt.BrKartice = brKartice;
                }

                ctx.Kartica.Add(kartica);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kartica uspješno dodana.";
                return RedirectToAction("Kartice", new { page = page, sort = sort, ascending = ascending });
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// POST metoda za brisanje kartice.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>Vraća redirekt ili HTTP status kod.</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteKartica(int brKartice)
        {
            try
            {
                var kartica = await ctx.Kartica.FindAsync(brKartice);
                var projekti = await ctx.Projekt.Where(p => p.BrKartice == brKartice).AsNoTracking().ToListAsync();

                foreach (var projekt in projekti)
                {
                    projekt.BrKartice = null;
                }

                if (kartica == null)
                {
                    // Return a 404 Not Found status code with a message
                    TempData["NotFoundMessage"] = "Zahtjev ne postoji";
                    return NotFound();
                }

                var transakcije = await ctx.Transakcija.Where(t => t.BrKartice == brKartice).ToListAsync();

                foreach (var trans in transakcije)
                {
                    ctx.Transakcija.Remove(trans);
                }

                ctx.Kartica.Remove(kartica);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kartica uspješno izbrisana";

                return RedirectToAction("Kartice");
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Prikaz Master-detail stranice za karticu.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>View s prikazom master-detail forme.</returns>
        [HttpGet]
        public async Task<IActionResult> KarticeMD(int brKartice)
        {
            try
            {
                var kartica = await ctx.Kartica
                    .Where(k => k.BrKartice == brKartice)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (kartica == null)
                {
                    return NotFound();
                }

                var projekt = await ctx.Projekt
                    .Where(p => p.BrKartice == brKartice)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                var transakcije = await ctx.Transakcija
                    .Where(t => t.BrKartice == brKartice)
                    .Include(t => t.IdVrsteNavigation)
                    .AsNoTracking()
                    .ToListAsync();

                KarticaViewModel model = new KarticaViewModel(kartica, projekt, transakcije);

                return View(model);

            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }

        }

        /// <summary>
        /// Funkcija za prijlaz na slijedeću karticu u MD prikazu.
        /// </summary>
        /// <param name="currentBrKartice">Trenutna kartica.</param>
        /// <returns>View sa MD formom slijedeće kartice.</returns>
        public async Task<IActionResult> SljedecaKartica(int currentBrKartice)
        {
            try
            {
                var currentKartica = await ctx.Kartica
                    .Where(k => k.BrKartice == currentBrKartice)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (currentKartica == null)
                {
                    return NotFound("Current Kartica not found.");
                }

                var kartice = await ctx.Kartica.OrderBy(k => k.BrKartice).ToListAsync();

                int index = kartice.FindIndex(k => k.BrKartice == currentBrKartice);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Kartica.");
                }

                int nextBrKartica = kartice[(index + 1) % kartice.Count()].BrKartice;

                return RedirectToAction("KarticeMD", new { brKartice = nextBrKartica });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        /// <summary>
        /// Funkcija za prijlaz na prethodnu karticu u MD prikazu.
        /// </summary>
        /// <param name="currentBrKartice">Trenutna kartica.</param>
        /// <returns>View sa MD formom prethodne kartice.</returns>
        [HttpGet]
        public async Task<IActionResult> PrethodnaKartica(int currentBrKartice)
        {
            try
            {
                var currentKartica = await ctx.Kartica
                    .Where(k => k.BrKartice == currentBrKartice)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (currentKartica == null)
                {
                    return NotFound("Current Kartica not found.");
                }

                var kartice = await ctx.Kartica.OrderBy(k => k.BrKartice).ToListAsync();

                int index = kartice.FindIndex(k => k.BrKartice == currentBrKartice);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Kartica.");
                }

                int newIndex = (index - 1) % kartice.Count();

                newIndex = newIndex < 0 ? kartice.Count() - 1 : newIndex;

                int prevBrKartice = kartice[newIndex].BrKartice;

                return RedirectToAction("KarticeMD", new { brKartice = prevBrKartice });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        /// <summary>
        /// Prikaz redka u tablici kartica.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>Partial view s jednik retkom tablice.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(int brKartice)
        {
            var kartica = await ctx.Kartica
                .Where(k => k.BrKartice == brKartice)
                .Select(k => new KarticeViewModel
                {
                    BrKartice = k.BrKartice,
                    Stanje = k.Stanje,
                    Projekt = k.Projekt
                })
                .SingleOrDefaultAsync();

            if (kartica != null)
            {
                return PartialView(kartica);
            }
            else
            {
                return NotFound($"Neispravan broj kartice: {brKartice}");
            }
        }

        /// <summary>
        /// Prikaz redka za ažuriranje podatka kartice.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>Partial view retka za ažuriranje kartice.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int brKartice)
        {
            var kartica = await ctx.Kartica
                .Include(k => k.Projekt)
                .AsNoTracking()
                .Where(k => k.BrKartice == brKartice)
                .SingleOrDefaultAsync();

            if (kartica != null)
            {
                return PartialView(kartica);
            }
            else
            {
                logger.LogWarning("Ne postoji kartica: {0} ", brKartice);
                return NotFound($"Neispravan broj kartice: {brKartice}");
            }
        }

        /// <summary>
        /// POST metoda za ažuriranje kartice unutar retka.
        /// </summary>
        /// <param name="kartica">Objekt kartice.</param>
        /// <returns>Redirekt ako je sve prošlo dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit(Kartica kartica)
        {

            if (kartica == null)
            {
                logger.LogWarning("Ne postoji kartica: {0} ", kartica.BrKartice);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.Kartica.AnyAsync(k => k.BrKartice == kartica.BrKartice);
            if (!checkId)
            {
                return NotFound($"Neispravan broj kartice: {kartica?.BrKartice}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(kartica);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Kartica azurirana - Broj kartice: {kartica.BrKartice}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Kartica {kartica.BrKartice} azurirana.");
                    return RedirectToAction(nameof(Get), new { brKartice = kartica.BrKartice });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(kartica);
                }
            }
            else
            {
                return PartialView(kartica);
            }
        }

        /// <summary>
        /// Funkcija za brisanje kartice.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>Vraća poruku o izbrisanoj kartici.</returns>
        [HttpDelete]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(int brKartice)
        {
            ActionResponseMessage responseMessage;
            var kartica = await ctx.Kartica.FindAsync(brKartice);
            if (kartica != null)
            {
                try
                {
                    ctx.Remove(kartica);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Kartica izbrisana - Br. kartice: {brKartice}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Kartica {brKartice} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"Kartica {brKartice} uspješno obrisana.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja kartice: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja kartice: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Kartica s brojem {brKartice} ne postoji");
                logger.LogWarning("Ne postoji kartica s brojem: {0} ", brKartice);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(brKartice);
        }

        /// <summary>
        /// Uvoz kartica kroz excel datoteku.
        /// </summary>
        /// <param name="file">Excel file.</param>
        /// <returns>Redirekt ako je sve prošlo dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> ImportKarticaExcel(IFormFile file)
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

                        List<Kartica> kartice = new List<Kartica>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var kartica = new Kartica
                            {
                                BrKartice = int.TryParse(worksheet.Cells[row, 1].Value?.ToString(), out int brKartice)
                                    ? brKartice
                                    : 0,
                                Stanje = decimal.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out decimal stanje)
                                    ? stanje
                                    : 0.0m
                            };

                            kartice.Add(kartica);
                        }

                        ctx.AddRange(kartice);
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
