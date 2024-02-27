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
using OfficeOpenXml;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler za upravljanje operacijama vezanim uz uloge.
    /// </summary>
    public class UlogaController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly ILogger<UlogaController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor za UlogaController.
        /// </summary>
        /// <param name="ctx">Objekt za pristup bazi podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zapisivanje događaja.</param>

        public UlogaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<UlogaController> logger)
        {
            this._context = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// Prikazuje popis uloga s opcijama za straničenje i sortiranje.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Pogled s popisom uloga.</returns>

        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = _context.Uloga.AsNoTracking();



            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedna uloga");
                TempData[Constants.Message] = "Ne postoji nijedna uloga.";
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

            /*var uloge = query.Skip((page - 1) * pagesize).Take(pagesize).ToList(); */

            var uloge = await query
                          .Select(m => new UlogeViewModel
                          {
                              IdUloge = m.IdUloge,
                              ImeUloge = m.ImeUloge,
                              Opis = m.Opis
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new UlogaViewModel
            {
                uloge = uloge,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Dohvaća detalje o određenoj ulozi.
        /// </summary>
        /// <param name="id">Identifikator uloge.</param>
        /// <returns>Pogled s detaljima o ulozi.</returns>


        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var uloga = await _context.Uloga
                                  .Where(m => m.IdUloge == id)
                                  .Select(m => new UlogeViewModel
                                  {
                                      IdUloge = m.IdUloge,
                                      ImeUloge = m.ImeUloge,
                                      Opis = m.Opis,
                                      IdVrste = m.IdVrste,
                                  })
                                  .SingleOrDefaultAsync();
            if (uloga != null)
            {
                return PartialView(uloga);
            }
            else
            {
                return NotFound($"Neispravan id uloge: {id}");
            }
        }

        /// <summary>
        /// Prikazuje formu za stvaranje nove uloge.
        /// </summary>
        /// <returns>Pogled s formom za stvaranje nove uloge.</returns>



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<VrstaUloge> vrsteLista = _context.VrstaUloge.AsNoTracking().ToList();

            var viewModel = new UlogaViewModel
            {
                VrsteUloga = vrsteLista
            };

            return View(viewModel);
        }

        /// <summary>
        /// Stvara novu ulogu na temelju podataka iz obrasca.
        /// </summary>
        /// <param name="viewModel">Model za novu ulogu.</param>
        /// <returns>Pogled s rezultatom stvaranja uloge.</returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UlogaViewModel viewModel)
        {
            Uloga uloga = viewModel.Uloga;

            logger.LogTrace(JsonSerializer.Serialize(uloga));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(uloga);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Uloga stvorena - Id: {uloga.IdUloge}, Ime: {uloga.ImeUloge}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    _context.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"Uloga {uloga.ImeUloge} dodana.");

                    TempData[Constants.Message] = $"Uloga {uloga.ImeUloge} dodana.";
                    TempData["created"] = true;
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje nove osobe: {0}", exc.CompleteExceptionMessage());
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
            var uloga = _context.Uloga.AsNoTracking().Where(d => d.IdUloge.Equals(id)).SingleOrDefault();
            if (uloga == null)
            {
                logger.LogWarning("Ne postoji uloga s oznakom: {0} ", id);
                return NotFound("Ne postoji uloga s oznakom: " + id);
            }
            else
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                return View(uloga);
            }
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            try
            {
                Uloga uloga = await _context.Uloga.Where(d => d.IdUloge.Equals(id)).FirstOrDefaultAsync();

                if (uloga == null)
                {
                    return NotFound("Neispravna oznaka osobe: " + id);
                }

                if (await TryUpdateModelAsync<Uloga>(uloga, "",
                    d => d.IdUloge, d => d.ImeUloge, d => d.Opis
                ))
                {
                    ViewBag.Page = page;
                    ViewBag.Sort = sort;
                    ViewBag.Ascending = ascending;
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData[Constants.Message] = "Uloga ažurirana.";
                        TempData["updated"] = true;
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
                    }
                    catch (Exception exc)
                    {
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        return View(uloga);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Podatke o državi nije moguće povezati s forme");
                    return View(uloga);
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
        /// Prikazuje formu za uređivanje postojeće uloge.
        /// </summary>
        /// <param name="id">Identifikator uloge koja se uređuje.</param>
        /// <returns>Pogled s formom za uređivanje uloge.</returns>

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {

            var uloga = await _context.Uloga
                                  .AsNoTracking()
                                  .Where(m => m.IdUloge == id)
                                  .SingleOrDefaultAsync();
            if (uloga != null)
            {
                return PartialView(uloga);
            }
            else
            {
                logger.LogWarning("Ne postoji uloga s oznakom: {0} ", id);
                return NotFound($"Neispravan id uloge: {id}");
            }
        }

        /// <summary>
        /// Ažurira postojeću ulogu na temelju podataka iz obrasca.
        /// </summary>
        /// <param name="uloga">Uloga s ažuriranim podacima.</param>
        /// <returns>Rezultat ažuriranja uloge.</returns>

        [HttpPost]
        public async Task<IActionResult> Edit(Uloga uloga)
        {

            if (uloga == null)
            {
                logger.LogWarning("Ne postoji uloga s oznakom: {0} ", uloga.IdUloge);
                return NotFound("Nema poslanih podataka");

            }
            bool checkId = await _context.Uloga.AnyAsync(m => m.IdUloge == uloga.IdUloge);
            if (!checkId)
            {
                return NotFound($"Neispravan id uloge: {uloga?.IdUloge}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(uloga);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Uloga azurirana - Id: {uloga.IdUloge}, Ime: {uloga.ImeUloge}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Uloga {uloga.ImeUloge} azurirana.");
                    return RedirectToAction(nameof(Get), new { id = uloga.IdUloge });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(uloga);
                }
            }
            else
            {

                return PartialView(uloga);
            }
        }


        /*

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var uloga = _context.Uloga.Find(id);

            if (uloga != null)
            {
                bool ulogaUsed = false;
                if (_context.RadiNa.Any(p => p.IdProjekta == id)) ulogaUsed = true;
                if (_context.RadiNa.Any(p => p.IdOsoba == id)) ulogaUsed = true;

                if (ulogaUsed)
                {
                    TempData[Constants.Message] = "Osoba se koristi";
                    TempData[Constants.ErrorOccurred] = true;
                    TempData["used"] = true;
                }
                else
                {
                    try
                    {
                        string naziv = uloga.ImeUloge;
                        _context.Remove(uloga);
                        _context.SaveChanges();
                        logger.LogInformation($"Uloga {naziv} uspješno obrisana");
                        TempData["deleted"] = true;
                        TempData[Constants.Message] = $"Uloga {naziv} uspješno obrisana";
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
                logger.LogWarning("Ne postoji uloga s oznakom: {0} ", id);
                TempData[Constants.Message] = "Ne postoji uloga s oznakom: " + id;
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
        }
        */

        /// <summary>
        /// Briše određenu ulogu.
        /// </summary>
        /// <param name="id">Identifikator uloge koja se briše.</param>
        /// <returns>Rezultat brisanja uloge.</returns>

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var uloga = await _context.Uloga.FindAsync(id);
            if (uloga != null)
            {
                try
                {
                    string naziv = uloga.ImeUloge;
                    _context.Remove(uloga);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Uloga izbrisana - Id: {uloga.IdUloge}, Ime: {uloga.ImeUloge}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation($"Uloga {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"uloga {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja uloge: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja uloge: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"uloga sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji uloga s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// Uvozi podatke o ulogama iz Excel datoteke.
        /// </summary>
        /// <param name="file">Excel datoteka s podacima o ulogama.</param>
        /// <returns>Rezultat uvoza podataka.</returns>

        [HttpPost]
        public async Task<IActionResult> ImportUlogaExcel(IFormFile file)
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

                        List<Uloga> vrsteUlogaList = new List<Uloga>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new Uloga
                            {
                                ImeUloge = worksheet.Cells[row, 1].Value?.ToString(),
                                Opis = worksheet.Cells[row, 3].Value?.ToString(),
                                IdVrste = 1
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