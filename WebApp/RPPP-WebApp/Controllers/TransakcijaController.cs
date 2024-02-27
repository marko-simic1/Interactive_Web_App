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
using OfficeOpenXml;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler za upravljanje operacijama vezanim uz entitet transakcije.
    /// </summary>
    public class TransakcijaController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<TransakcijaController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor za TransakcijaController.
        /// </summary>
        /// <param name="ctx">Objekt za pristup bazi podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zapisivanje događaja.</param>
        public TransakcijaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<TransakcijaController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// Prikazuje popis transakcija s opcijama za straničenje i sortiranje.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s tablicom transakcija.</returns>
        [HttpGet]
        public IActionResult Transakcije(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
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

            var transakcije = query
                .Include(t => t.IdVrsteNavigation)
                .Select(t => new TransakcijeViewModel
                {
                    Model = t.Model,
                    PozivNaBr = t.PozivNaBr,
                    IdTrans = t.IdTrans,
                    Opis = t.Opis,
                    Iznos = t.Iznos,
                    Datum = t.Datum,
                    IdVrsteNavigation = t.IdVrsteNavigation,
                    BrKartice = t.BrKartice
                })
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            var model = new TransakcijaViewModel
            {
                transakcije = transakcije,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Prikazuje stranicu za ažuriranje transakcije.
        /// </summary>
        /// <param name="id">Broj kartice</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <param name="redirect">Varijable za vraćanje na prethodnu stranicu.</param>
        /// <returns>View s formom za ažuriranje kartice.</returns>
        [HttpGet]
        public async Task<IActionResult> FormUpdateTransakcija(int redirect, int id, int CurrentPage, int Sort, bool Ascending)
        {
            var transakcija = await ctx.Transakcija
                .Where(t => t.IdTrans == id)
                .Include(t => t.BrKarticeNavigation)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var kartice = await ctx.Kartica.AsNoTracking().ToListAsync();
            var vrste = ctx.VrstaTransakcije.AsNoTracking().ToList();

            if (transakcija == null)
            {
                return NotFound();
            }

            var viewModel = new TransakcijaViewModel(transakcija, kartice, vrste, redirect);
            viewModel.PagingInfo = new PagingInfo { Ascending = Ascending, CurrentPage = CurrentPage, Sort = Sort };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za ažuriranje transakcije.
        /// </summary>
        /// <param name="redirect">Varijable za vraćanje na prethodnu stranicu.</param>
        /// <param name="id">Id transakcije.</param>
        /// <param name="brKartice">Broj kartice.</param>
        /// <param name="model">Model transakcije.</param>
        /// <param name="opis">Opis transakcije.</param>
        /// <param name="iznos">Iznos transakcije.</param>
        /// <param name="datum">Datum transakcije.</param>
        /// <param name="pozivNaBr">Poziv na broj transakcije.</param>
        /// <param name="idVrste">ID vrste transakcije.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Redirekt ili HTTP status kod.</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateTransakcija(int redirect, int id, int brKartice, string model, string opis, decimal iznos, DateTime datum, int pozivNaBr, int idVrste, int page, int sort, bool ascending)
        {
            try
            {
                var existingTransakcija = await ctx.Transakcija.FindAsync(id);

                if (existingTransakcija == null)
                {
                    return NotFound();
                }

                existingTransakcija.Model = model;
                existingTransakcija.Opis = opis;
                existingTransakcija.Iznos = iznos;
                existingTransakcija.Datum = datum;
                existingTransakcija.PozivNaBr = pozivNaBr;
                existingTransakcija.IdVrste = idVrste;
                existingTransakcija.BrKartice = brKartice;

                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Transakcija uspješno ažurirana.";
                if(redirect == 1)
                {
                    return RedirectToAction("Transakcije", new { page = page, sort = sort, ascending = ascending });
                }
                else
                {
                    return RedirectToAction("KarticeMD", "Kartica", new { brKartice = brKartice });
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, log the error, or return an appropriate response
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Prikaz forme za kreiranje nove transakcije.
        /// </summary>
        /// <param name="brKartice">Broj kartice.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s prikazom forme za dodavanje nove transakcije.</returns>
        [HttpGet]
        public IActionResult FormCreateTransakcija(int brKartice, int page, int sort, bool ascending)
        {
            var kartice = ctx.Kartica.AsNoTracking().ToList();
            var vrste = ctx.VrstaTransakcije.AsNoTracking().ToList();

            var transakcija = new Transakcija { BrKartice = brKartice };

            TransakcijaViewModel viewModel = new TransakcijaViewModel(transakcija, kartice, vrste);

            viewModel.PagingInfo = new PagingInfo { CurrentPage = page, Sort = sort, Ascending = ascending };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za kreiranje nove transakcije.
        /// </summary>
        /// <param name="redirect">Varijable za vraćanje na prethodnu stranicu.</param>
        /// <param name="id">Id transakcije.</param>
        /// <param name="brKartice">Broj kartice.</param>
        /// <param name="model">Model transakcije.</param>
        /// <param name="opis">Opis transakcije.</param>
        /// <param name="iznos">Iznos transakcije.</param>
        /// <param name="datum">Datum transakcije.</param>
        /// <param name="pozivNaBr">Poziv na broj transakcije.</param>
        /// <param name="idVrste">ID vrste transakcije.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Redirekt ili HTTP status kod.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateTransakcija(int redirect, int brKartice, string model, string opis, decimal iznos, DateTime datum, int pozivNaBr, int idVrste, int page, int sort, bool ascending)
        {
            try
            {
                Transakcija transakcija = new Transakcija();
                transakcija.Model = model;
                transakcija.Opis = opis;
                transakcija.Iznos = iznos;
                transakcija.Datum = datum;
                transakcija.PozivNaBr = pozivNaBr;
                transakcija.IdVrste = idVrste;
                transakcija.BrKartice = brKartice;

                ctx.Transakcija.Add(transakcija);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Transakcija uspješno dodana.";
                if(redirect == 1)
                {
                    return RedirectToAction("Transakcije", new { page = page, sort = sort, ascending = ascending });
                }
                else
                {
                    return RedirectToAction("KarticeMD", "Kartica", new { brKartice = brKartice });
                }
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Funkcija za brisanje transakcije.
        /// </summary>
        /// <param name="id">ID transakcije.</param>
        /// <param name="redirect">Redirekt na stranicu.</param>
        /// <param name="brKartice">Broj kartice.</param>
        /// <returns>Vraća redirekt.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteTransakcija(int id, int redirect, int brKartice)
        {
            try
            {
                var transakcija = await ctx.Transakcija.FindAsync(id);

                if (transakcija == null)
                {
                    TempData["NotFoundMessage"] = "Transakcija ne postoji";
                    return NotFound();
                }

                ctx.Transakcija.Remove(transakcija);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Transakcija uspješno izbrisana";
                if(redirect == 1)
                {
                    return RedirectToAction("Transakcije");
                }
                else
                {
                    return RedirectToAction("KarticeMD", "Kartica", new { brKartice = brKartice });
                }
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Prikaz vrsta transakcije.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s prikazom vrsta transakcije.</returns>
        [HttpGet]
        public IActionResult VrsteTransakcija(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.VrstaTransakcije.AsNoTracking();

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

            var vrste = query
                .Select(v => new VrsteTransakcijaViewModel
                {
                    IdVrste = v.IdVrste,
                    ImeVrste = v.ImeVrste
                })
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            var model = new VrstaTransakcijeViewModel
            {
                vrste = vrste,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Prikaz forme za kreiranje nove vrste transakcije.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s formom za kreiranje nove vrste transakcije.</returns>
        [HttpGet]
        public IActionResult FormCreateVrstaTransakcije(int page, int sort, bool ascending)
        {

            VrstaTransakcijeViewModel viewModel = new VrstaTransakcijeViewModel();

            viewModel.PagingInfo = new PagingInfo { CurrentPage = page, Sort = sort, Ascending = ascending };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za kreiranje nove vrste transakcije.
        /// </summary>
        /// <param name="imeVrste">Ime vrste transakcije.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Redirekt ako je sve prošlo dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateVrstaTransakcije(string imeVrste, int page, int sort, bool ascending)
        {
            try
            {
                VrstaTransakcije vrsta = new VrstaTransakcije();
                vrsta.ImeVrste = imeVrste;
    
                ctx.VrstaTransakcije.Add(vrsta);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vrsta transakcije uspješno dodana.";
                return RedirectToAction("VrsteTransakcija", new { page = page, sort = sort, ascending = ascending });
                
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Prikaz forme za ažuriranje vrste transakcije.
        /// </summary>
        /// <param name="id">ID transakcije.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s prikazom forme za ažuriranje vrste transakcije.</returns>
        [HttpGet]
        public async Task<IActionResult> FormUpdateVrstaTransakcije(int id, int CurrentPage, int Sort, bool Ascending)
        {
            var vrsta = await ctx.VrstaTransakcije
                .Where(v => v.IdVrste == id)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (vrsta == null)
            {
                return NotFound();
            }

            var viewModel = new VrstaTransakcijeViewModel(vrsta);
            viewModel.PagingInfo = new PagingInfo { Ascending = Ascending, CurrentPage = CurrentPage, Sort = Sort };

            return View(viewModel);
        }

        /// <summary>
        /// POST metoda za ažuriranje vrste transakcije.
        /// </summary>
        /// <param name="id">ID vrste transakcije.</param>
        /// <param name="imeVrste">Ime vrste transakcije.</param>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Vraća redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateVrstaTransakcije(int id, string imeVrste, int page, int sort, bool ascending)
        {
            try
            {
                var existingVrsta = await ctx.VrstaTransakcije.FindAsync(id);

                if (existingVrsta == null)
                {
                    return NotFound();
                }

                existingVrsta.ImeVrste = imeVrste;

                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vrsta transakcije uspješno ažurirana.";
                return RedirectToAction("VrsteTransakcija", new { page = page, sort = sort, ascending = ascending });
                
            }
            catch (Exception ex)
            {
                // Handle exceptions, log the error, or return an appropriate response
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Funkcija za brisanje vrste transakcije.
        /// </summary>
        /// <param name="id">ID vrste transakcije.</param>
        /// <returns>Vraća redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteVrstaTransakcije(int id)
        {
            try
            {
                var vrsta = await ctx.VrstaTransakcije.FindAsync(id);

                if (vrsta == null)
                {
                    TempData["NotFoundMessage"] = "Vrsta transakcija ne postoji";
                    return NotFound();
                }

                var transakcije = await ctx.Transakcija
                    .Where(t => t.IdVrste == id)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var transakcija in transakcije)
                {
                    transakcija.IdVrste = 0;
                }

                ctx.VrstaTransakcije.Remove(vrsta);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vrsta transakcije uspješno izbrisana";
                return RedirectToAction("VrsteTransakcija");
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Prikaz redka u tablici transakcije.
        /// </summary>
        /// <param name="id">ID transakcije.</param>
        /// <returns>Partial view redka transakcije.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTransakcija(int id)
        {
            var transakcija = await ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
                .Where(t => t.IdTrans == id)
                .Select(t => new TransakcijeViewModel
                {
                    Model = t.Model,
                    PozivNaBr = t.PozivNaBr,
                    IdTrans = t.IdTrans,
                    Opis = t.Opis,
                    Iznos = t.Iznos,
                    Datum = t.Datum,
                    IdVrsteNavigation = t.IdVrsteNavigation,
                    BrKartice = t.BrKartice
                })
                .SingleOrDefaultAsync();

            if (transakcija != null)
            {
                return PartialView(transakcija);
            }
            else
            {
                return NotFound($"Neispravan id: {id}");
            }
        }

        /// <summary>
        /// Prikaz forme za ažuriranje retka u tablici.
        /// </summary>
        /// <param name="id">ID transakcije.</param>
        /// <returns>Partial view retka za ažuriranje transakcije.</returns>
        [HttpGet]
        public async Task<IActionResult> EditTransakcija(int id)
        {
            var transakcija = await ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
                .AsNoTracking()
                .Where(t => t.IdTrans == id)
                .SingleOrDefaultAsync();

            if (transakcija != null)
            {
                return PartialView(transakcija);
            }
            else
            {
                logger.LogWarning("Ne postoji id: {0} ", id);
                return NotFound($"Neispravan id: {id}");
            }
        }

        /// <summary>
        /// Ažuriranje retka u tablici transakcija
        /// </summary>
        /// <param name="transakcija">Objekt transakcije.</param>
        /// <returns>Redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> EditTransakcija(Transakcija transakcija)
        {

            if (transakcija == null)
            {
                logger.LogWarning("Ne postoji transakcija: {0} ", transakcija.IdTrans);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.Transakcija.AnyAsync(t => t.IdTrans == transakcija.IdTrans);
            if (!checkId)
            {
                return NotFound($"Neispravan broj kartice: {transakcija?.IdTrans}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(transakcija);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Transakcija azurirana - ID kartice: {transakcija.IdTrans}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Transakcija {transakcija.IdTrans} azurirana.");
                    return RedirectToAction(nameof(GetTransakcija), new { id = transakcija.IdTrans });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(transakcija);
                }
            }
            else
            {
                return PartialView(transakcija);
            }
        }

        /// <summary>
        /// Prikaz retka vrste transakcije u tablici.
        /// </summary>
        /// <param name="id">ID vrste transakcije.</param>
        /// <returns>Partial view retka vrste transkacije.</returns>
        [HttpGet]
        public async Task<IActionResult> GetVrsta(int id)
        {
            var vrsta = await ctx.VrstaTransakcije
                .Where(v => v.IdVrste == id)
                .Select(v => new VrsteTransakcijaViewModel
                {
                    IdVrste = v.IdVrste,
                    ImeVrste = v.ImeVrste
                })
                .SingleOrDefaultAsync();

            if (vrsta != null)
            {
                return PartialView(vrsta);
            }
            else
            {
                return NotFound($"Neispravan id: {id}");
            }
        }

        /// <summary>
        /// Prikaz forme za ažuriranje retka u tablici vrsta transakcije.
        /// </summary>
        /// <param name="id">ID vrste transakcije.</param>
        /// <returns>Partial view forme za ažuriranje retka transakcije.</returns>
        [HttpGet]
        public async Task<IActionResult> EditVrsta(int id)
        {
            var vrsta = await ctx.VrstaTransakcije
                .AsNoTracking()
                .Where(v => v.IdVrste == id)
                .SingleOrDefaultAsync();

            if (vrsta != null)
            {
                return PartialView(vrsta);
            }
            else
            {
                logger.LogWarning("Ne postoji id: {0} ", id);
                return NotFound($"Neispravan id: {id}");
            }
        }

        /// <summary>
        /// Metoda za ažuriranje retka vrste transakcije.
        /// </summary>
        /// <param name="vrsta">Objekt vrste transakcije.</param>
        /// <returns>Redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> EditVrsta(VrstaTransakcije vrsta)
        {

            if (vrsta == null)
            {
                logger.LogWarning("Ne postoji vrsta transakcije: {0} ", vrsta.IdVrste);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.VrstaTransakcije.AnyAsync(v => v.IdVrste == vrsta.IdVrste);
            if (!checkId)
            {
                return NotFound($"Neispravan id: {vrsta?.IdVrste}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta transakcije azurirana - ID: {vrsta.IdVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Vrsta transakcije {vrsta.IdVrste} azurirana.");
                    return RedirectToAction(nameof(GetVrsta), new { id = vrsta.IdVrste});
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(vrsta);
                }
            }
            else
            {
                return PartialView(vrsta);
            }
        }

        /// <summary>
        /// Uvoz transakcija kroz excel datoteku.
        /// </summary>
        /// <param name="file">Excel datoteka.</param>
        /// <returns>Redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> ImportTransakcijaExcel(IFormFile file)
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

                        List<Transakcija> transakcije = new List<Transakcija>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var transakcija = new Transakcija
                            {
                                Model = worksheet.Cells[row, 1].Value?.ToString(),
                                PozivNaBr = int.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out int pozivNaBr)
                                    ? pozivNaBr
                                    : 0,
                                Opis = worksheet.Cells[row, 3].Value?.ToString(),
                                BrKartice = int.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out int brKartice)
                                    ? brKartice
                                    : 0,
                                Iznos = decimal.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out decimal iznos)
                                    ? iznos
                                    : 0,

                            };

                            transakcije.Add(transakcija);
                        }

                        ctx.AddRange(transakcije);
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

        /// <summary>
        /// Uvoz vrsta transakcija kroz excel datoteku.
        /// </summary>
        /// <param name="file">Excel datoteka.</param>
        /// <returns>Redirekt ako sve prođe dobro.</returns>
        [HttpPost]
        public async Task<IActionResult> ImportVrstaExcel(IFormFile file)
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

                        List<VrstaTransakcije> vrste = new List<VrstaTransakcije>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrsta = new VrstaTransakcije
                            {
                                ImeVrste = worksheet.Cells[row, 1].Value?.ToString()

                            };

                            vrste.Add(vrsta);
                        }

                        ctx.AddRange(vrste);
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
