using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using RPPP_WebApp;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Text.Json;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using Azure;
using Microsoft.Data.SqlClient;
using NLog.Fluent;
using OfficeOpenXml;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Controller za obavljanje operacija nad projektima i za prikaz Razor View-a vezanih uz projekte.
    /// </summary>
    public class ProjekttController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<PartnerController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor razreda ProjekttController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>
        public ProjekttController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<PartnerController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }
        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih projekata u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        /// 
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = ctx.Projekt.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedan projekt");
                TempData[Constants.Message] = "Ne postoji nijedan projekt.";
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

            /*var partneri = query.Skip((page - 1) * pagesize).Take(pagesize).ToList(); */

            var projekti = await query
                          .Select(m => new ProjektiiViewModel
                          {
                              IdProjekta = m.IdProjekta,
                              ImeProjekta = m.ImeProjekta,
                              Kratica = m.Kratica,
                              Sazetak = m.Sazetak,
                              DatumPoc = m.DatumPoc,
                              DatumZav=m.DatumZav,
                              BrKartice = m.BrKartice,
                              IdVrste = m.IdVrste,
                             
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new ProjekttViewModel
            {
                projekti= projekti,
                PagingInfo = pagingInfo
            };

            return View(model);
        }
        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz projekte.
        /// </summary>
        /// <param name="id">Projekt koji se traži.</param>
        /// <returns>vraća prikaz sadržaja projekta. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var projekti = await ctx.Projekt
                                  .Where(m => m.IdProjekta == id)
                                  .Select(m => new ProjektiiViewModel
                                  {
                                      IdProjekta = m.IdProjekta,
                                      ImeProjekta = m.ImeProjekta,
                                      Kratica = m.Kratica,
                                      Sazetak = m.Sazetak,
                                      DatumPoc = m.DatumPoc,
                                      DatumZav = m.DatumZav,
                                      BrKartice = m.BrKartice,
                                      IdVrste = m.IdVrste,
                                  })
                                  .SingleOrDefaultAsync();
            if (projekti != null)
            {
                return PartialView(projekti);
            }
            else
            {
                return NotFound($"Neispravan id projekta: {id}");
            }
        }


        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem novog projekta.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje projekta. </returns>



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Projekt> vrsteLista = ctx.Projekt.AsNoTracking().ToList();

            var viewModel = new ProjekttViewModel
            {
                Projekti = vrsteLista
            };

            return View(viewModel);
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem novog projekta.
        /// </summary>
        /// <param name="viewModel">ViewModel u koji unosimo projekt.</param>
        /// <returns>Redirect na početnu stranicu projekta. </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjekttViewModel viewModel)
        {
            Projekt projekt = viewModel.Projekt;

            logger.LogTrace(JsonSerializer.Serialize(projekt));
            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Add(projekt);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Partner dodan - Id: {projekt.IdPartnera}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    ctx.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"Partner {projekt.ImeProjekta} dodan.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"Partner {projekt.ImeProjekta} dodan.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje novog projekta: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    return View(viewModel);
                }
            }
            else
            {
                return View(viewModel);
            }
        }




        /*
         
        [HttpGet]
        public IActionResult Edit(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var partner = _context.Partner.AsNoTracking().Where(d => d.IdPartnera.Equals(id)).SingleOrDefault();
            if (partner == null)
            {
                logger.LogWarning("Ne postoji partner s oznakom: {0} ", id);
                return NotFound("Ne postoji partner s oznakom: " + id);
            }
            else
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                return View(partner);
            }
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            try
            {
                Partner partner = await _context.Partner.Where(d => d.IdPartnera.Equals(id)).FirstOrDefaultAsync();

                if (partner == null)
                {
                    return NotFound("Neispravna oznaka osobe: " + id);
                }

                if (await TryUpdateModelAsync<Partner>(partner, "",
                    d => d.Ime, d => d.Oib, d => d.Telefon, d => d.Email, d => d.Iban, d => d.Adresa
                ))
                {
                    ViewBag.Page = page;
                    ViewBag.Sort = sort;
                    ViewBag.Ascending = ascending;
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData[Constants.Message] = "Partner ažuriran.";
                        TempData["updated"] = true;
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
                    }
                    catch (Exception exc)
                    {
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        return View(partner);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "...");
                    return View(partner);
                }
            }
            catch (Exception exc)
            {
                TempData[Constants.Message] = exc.CompleteExceptionMessage();
                TempData[Constants.ErrorOccurred] = true;
                return RedirectToAction(nameof(Edit), id);
            }
        }

        */
        /// <summary>
        /// End point na koje se šalje zahtjev za formom za ažuriranje projekta.
        /// </summary>
        /// <param name="id">Id projekta kojeg želimo ažurirati.</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima projekta.</returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var projekt = await ctx.Projekt
                                  .AsNoTracking()
                                  .Where(m => m.IdProjekta == id)
                                  .SingleOrDefaultAsync();
            if (projekt != null)
            {
                return PartialView(projekt);
            }
            else
            {
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
                return NotFound($"Neispravan id projekta: {id}");
            }
        }
        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje projekta.
        /// </summary>
        /// <param name="projekt">Projekt koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// 
        [HttpPost]
        public async Task<IActionResult> Edit(Projekt projekt)
        {

            if (projekt == null)
            {
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", projekt.IdProjekta);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.Projekt.AnyAsync(m => m.IdProjekta == projekt.IdProjekta);
            if (!checkId)
            {
                return NotFound($"Neispravan id partnera: {projekt?.IdProjekta}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(projekt);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt azuriran - Id: {projekt.IdPartnera}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Projekt {projekt.ImeProjekta} azuriran.");
                    return RedirectToAction(nameof(Get), new { id = projekt.ImeProjekta });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(projekt);
                }
            }
            else
            {

                return PartialView(projekt);
            }
        }




        /*

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var partner = _context.Partner.SingleOrDefault(p => p.IdPartnera == id);

            if (partner != null)
            {
                bool partnerUsed = false;
                if (_context.Osoba.Any(p => p.IdOsoba == id)) partnerUsed = true;
                if (_context.Projekt.Any(z => z.IdProjekta == id)) partnerUsed = true;

                
                if (partnerUsed)
                {
                    TempData[Constants.Message] = "Partner se koristi";
                    TempData[Constants.ErrorOccurred] = true;
                    TempData["used"] = true;
                }
                else
                {
                    try
                    {
                        string naziv = partner.Ime;
                        _context.Remove(partner);
                        _context.SaveChanges();
                        logger.LogInformation($"Partner {naziv} uspješno obrisana");
                        TempData[Constants.Message] = $"Partner {naziv} uspješno obrisana";
                        TempData["deleted"] = true;
                        TempData[Constants.ErrorOccurred] = false;
                    }
                    catch (Exception exc)
                    {
                        TempData[Constants.Message] = "Pogreška prilikom brisanja osobe: " + exc.CompleteExceptionMessage();
                        TempData[Constants.ErrorOccurred] = true;
                        logger.LogError("Pogreška prilikom brisanja osobe: " + exc.CompleteExceptionMessage());
                    }
                }
            }
            else
            {
                logger.LogWarning("Ne postoji partner s oznakom: {0} ", id);
                TempData[Constants.Message] = "Ne postoji partner s oznakom: " + id;
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
        }

        */
        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem projekta.
        /// </summary>
        /// <param name="id">Projekt koja se briše.</param>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var projekt = await ctx.Projekt.FindAsync(id);
            if (projekt != null)
            {
                try
                {
                    string naziv = projekt.ImeProjekta;
                    ctx.Remove(projekt);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Projekt obrisan - Id: {id}, Ime: {projekt.ImeProjekta}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();

                    logger.LogInformation($"Projekt {naziv} uspješno obrisan");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"projekt {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja projekta: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja projekta: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"projekt sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji projekt s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }
        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem projekta putem excela.
        /// </summary>
        /// <param name="file">Datoteka koju pregledavamo.</param>
        /// <returns>Novu datoteku koja označava dodane stavke. </returns>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>

        [HttpPost]
        public async Task<IActionResult> ImportProjektExcel(IFormFile file)
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

                        List<Projekt> vrsteUlogaList = new List<Projekt>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new Projekt
                            {
                                ImeProjekta = worksheet.Cells[row, 1].Value?.ToString(),
                                Sazetak = worksheet.Cells[row, 2].Value?.ToString(),
                                Kratica = worksheet.Cells[row, 3].Value?.ToString(),
                                BrKartice = Convert.ToInt32(worksheet.Cells[row, 4].Value),
                                
                            };

                            vrsteUlogaList.Add(vrstaUloge);
                        }

                        ctx.AddRange(vrsteUlogaList);
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
