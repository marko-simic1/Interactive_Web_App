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

/// <summary>
/// Controller za obavljanje operacija nad vrstama uloge i za prikaz Razor View-a vezanih uz osobe.
/// </summary>
{
    public class VrstaUlogeController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly ILogger<VrstaUlogeController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor razreda VrstaUlogeController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>


        public VrstaUlogeController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<VrstaUlogeController> logger)
        {
            this._context = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih vrsta uloga u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        /// 
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = _context.VrstaUloge.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedna vrsta loge");
                TempData[Constants.Message] = "Ne postoji nijedna vrsta uloge.";
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

            /*var vrsteUloga = query.Skip((page - 1) * pagesize).Take(pagesize).ToList(); */

            var vrsteUloga = await query
                          .Select(m => new VrsteUlogaViewModel
                          {
                              IdVrste = m.IdVrste,
                              ImeVrste = m.ImeVrste
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new VrstaUlogeViewModel
            {
                vrste = vrsteUloga,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz vrstu uloge.
        /// </summary>
        /// <param name="id">Vrsta uloge koji se traži.</param>
        /// <returns>vraća prikaz sadržaja vrste uloge. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var vrsta = await _context.VrstaUloge
                                  .Where(m => m.IdVrste == id)
                                  .Select(m => new VrsteUlogaViewModel
                                  {
                                      IdVrste = m.IdVrste,
                                      ImeVrste = m.ImeVrste
                                  })
                                  .SingleOrDefaultAsync();
            if (vrsta != null)
            {
                return PartialView(vrsta);
            }
            else
            {
                return NotFound($"Neispravan id vrste: {id}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem nove vrste uloge.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje vrste uloge. </returns>


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem nove vrste uloge.
        /// </summary>
        /// <param name="vrsta">ViewModel u koji unosimo vrstu uloge.</param>
        /// <returns>Redirect na početnu stranicu vrste uloge. </returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VrstaUloge vrsta)
        {

            logger.LogTrace(JsonSerializer.Serialize(vrsta));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(vrsta);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta uloge stvorena - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    _context.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"VrstaUloge {vrsta.ImeVrste} dodana.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"VrstaUloge {vrsta.ImeVrste} dodana.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje nove osobe: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    return View(vrsta);
                }
            }
            else
            {
                return View(vrsta);
            }
        }



        /*

        [HttpGet]
        public IActionResult Edit(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var vrstaUloge = _context.VrstaUloge.AsNoTracking().Where(d => d.IdVrste.Equals(id)).SingleOrDefault();
            if (vrstaUloge == null)
            {
                logger.LogWarning("Ne postoji vrstaUloge s oznakom: {0} ", id);
                return NotFound("Ne postoji vrstaUloge s oznakom: " + id);
            }
            else
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                return View(vrstaUloge);
            }
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            try
            {
                VrstaUloge vrstaUloge = await _context.VrstaUloge.Where(d => d.IdVrste.Equals(id)).FirstOrDefaultAsync();

                if (vrstaUloge == null)
                {
                    return NotFound("Neispravna oznaka osobe: " + id);
                }

                if (await TryUpdateModelAsync<VrstaUloge>(vrstaUloge, "",
                    d => d.IdVrste, d => d.ImeVrste
                ))
                {
                    ViewBag.Page = page;
                    ViewBag.Sort = sort;
                    ViewBag.Ascending = ascending;
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData[Constants.Message] = "VrstaUloge ažurirana.";
                        TempData["updated"] = true;
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
                    }
                    catch (Exception exc)
                    {
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        return View(vrstaUloge);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "...");
                    return View(vrstaUloge);
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
        /// End point na koje se šalje zahtjev za formom za ažuriranje vrste uloge.
        /// </summary>
        /// <param name="id">Id vrste uloge kojeg želimo ažurirati.</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima vrste uloge.</returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vrsta = await _context.VrstaUloge
                                  .AsNoTracking()
                                  .Where(m => m.IdVrste == id)
                                  .SingleOrDefaultAsync();
            if (vrsta != null)
            {
                return PartialView(vrsta);
            }
            else
            {
                logger.LogWarning("Ne postoji vrsta uloge s oznakom: {0} ", id);
                return NotFound($"Neispravan id vrste: {id}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje vrste uloge.
        /// </summary>
        /// <param name="vrsta">Vrsta uloge koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


        [HttpPost]
        public async Task<IActionResult> Edit(VrstaUloge vrsta)
        {

            if (vrsta == null)
            {
                logger.LogWarning("Ne postoji vrsta uloge s oznakom: {0} ", vrsta.IdVrste);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await _context.VrstaUloge.AnyAsync(m => m.IdVrste == vrsta.IdVrste);
            if (!checkId)
            {
                return NotFound($"Neispravan id vrste: {vrsta?.IdVrste}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vrsta);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta uloge azurirana - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"vrsta uloge {vrsta.ImeVrste} azurirana.");
                    return RedirectToAction(nameof(Get), new { id = vrsta.IdVrste });
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


        /*

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var vrstaUloge = _context.VrstaUloge.Find(id);

            if (vrstaUloge != null)
            {
                bool vrstaUlogeUsed = false;

                if (vrstaUlogeUsed)
                {
                    TempData[Constants.Message] = "VrstaUloge se koristi";
                    TempData[Constants.ErrorOccurred] = true;
                    TempData["used"] = true;
                }
                else
                {
                    try
                    {
                        string naziv = vrstaUloge.ImeVrste;
                        _context.Remove(vrstaUloge);
                        _context.SaveChanges();
                        logger.LogInformation($"VrstaUloge {naziv} uspješno obrisana");
                        TempData[Constants.Message] = $"VrstaUloge {naziv} uspješno obrisana";
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
                logger.LogWarning("Ne postoji vrstaUloge s oznakom: {0} ", id);
                TempData[Constants.Message] = "Ne postoji vrstaUloge s oznakom: " + id;
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
        }
        */

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem vrste uloge.
        /// </summary>
        /// <param name="id">Osoba koja se briše.</param>

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await _context.VrstaUloge.FindAsync(id);
            if (vrsta != null)
            {
                try
                {
                    string naziv = vrsta.ImeVrste;
                    _context.Remove(vrsta);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta uloge izbrisana - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation($"vrsta uloge {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"vrsta {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja vrste: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja vrste uloge: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"vrsta sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji vrsta uloge s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem vrste uloge putem excela.
        /// </summary>
        /// <param name="file">Datoteka koju pregledavamo.</param>
        /// <returns>Novu datoteku koja označava dodane stavke. </returns>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>


        [HttpPost]
        public async Task<IActionResult> ImportVrsteUlogaExcel(IFormFile file)
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

                        List<VrstaUloge> vrsteUlogaList = new List<VrstaUloge>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new VrstaUloge
                            {
                                ImeVrste = worksheet.Cells[row, 1].Value?.ToString(),
                                IdVrste = Convert.ToInt32(worksheet.Cells[row, 2].Value),
                            };

                            vrsteUlogaList.Add(vrstaUloge);
                        }

                        _context.AddRange(vrsteUlogaList);
                        await _context.SaveChangesAsync();

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
