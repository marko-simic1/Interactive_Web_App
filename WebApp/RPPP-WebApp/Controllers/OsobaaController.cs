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
using NLog.Fluent;
using OfficeOpenXml;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler za upravljanje operacijama vezanim uz osobe.
    /// </summary>
    public class OsobaaController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly ILogger<OsobaController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor za OsobaaController.
        /// </summary>
        /// <param name="ctx">Objekt za pristup bazi podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zapisivanje događaja.</param>
        public OsobaaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<OsobaController> logger)
        {
            this._context = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }
        /// <summary>
        /// Prikazuje popis osoba s opcijama za straničenje i sortiranje.
        /// </summary>
        /// <param name="page">Trenutna stranica.</param>
        /// <param name="sort">Način sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>Pogled s popisom osoba.</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = _context.Osoba.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedna osoba");
                TempData[Constants.Message] = "Ne postoji nijedna osoba.";
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

            /*var osobe = query.Skip((page - 1) * pagesize).Take(pagesize).ToList();*/

            var osobe = await query
                          .Select(m => new OsobeViewModels
                          {
                              IdOsoba = m.IdOsoba,
                              Ime = m.Ime,
                              Prezime = m.Prezime,
                              Email = m.Email,
                              Iban = m.Iban,
                              Telefon = m.Telefon
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new OsobaViewModel
            {
                osobe = osobe,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Dohvaća detalje o određenoj osobi.
        /// </summary>
        /// <param name="id">Identifikator osobe.</param>
        /// <returns>Pogled s detaljima o osobi.</returns>

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var osoba = await _context.Osoba
                                  .Where(m => m.IdOsoba == id)
                                  .Select(m => new OsobeViewModels
                                  {
                                      IdOsoba = m.IdOsoba,
                                      Ime = m.Ime,
                                      Prezime = m.Prezime,
                                      Email = m.Email,
                                      Iban = m.Iban
                                  })
                                  .SingleOrDefaultAsync();
            if (osoba != null)
            {
                return PartialView(osoba);
            }
            else
            {
                return NotFound($"Neispravan id osobe: {id}");
            }
        }

        /// <summary>
        /// Prikazuje formu za stvaranje nove osobe.
        /// </summary>
        /// <returns>Pogled s formom za stvaranje nove osobe.</returns>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Osoba> osobe = _context.Osoba.AsNoTracking().ToList();


            var viewModel = new OsobaViewModel
            {
                Osobe = osobe,

            };

            return View(viewModel);
        }
        /// <summary>
        /// Stvara novu osobu na temelju podataka iz obrasca.
        /// </summary>
        /// <param name="viewModel">Model za novu osobu.</param>
        /// <returns>Pogled s rezultatom stvaranja osobe.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OsobaViewModel viewModel)
        {
            Osoba osoba = viewModel.Osoba;
            //int ulogaId = viewModel.UlogaId;

            logger.LogTrace(JsonSerializer.Serialize(osoba));

            if (ModelState.IsValid)
            {
                try
                {
                    int id;
                    do
                    {
                        id = new Random().Next();
                        var query = _context.Osoba.Select(o => o.IdOsoba).ToList();

                        if (!query.Contains(id))
                        {
                            break;
                        }

                    } while (true);

                    osoba.IdOsoba = id;
                    _context.Add(osoba);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Osoba dodana - Id: {id}, Ime: {osoba.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    _context.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"Osoba {osoba.Ime} dodana.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"Osoba {osoba.Ime} dodana.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje nove osobe: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    viewModel.partner = _context.Partner.AsNoTracking().ToList();
                    return View(viewModel);
                }
            }
            else
            {
                viewModel.partner = _context.Partner.AsNoTracking().ToList();
                return View(viewModel);
            }
        }


        /// <summary>
        /// Prikazuje formu za uređivanje postojeće osobe.
        /// </summary>
        /// <param name="id">Identifikator osobe koja se uređuje.</param>
        /// <returns>Pogled s formom za uređivanje osobe.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var osoba = await _context.Osoba
                                  .AsNoTracking()
                                  .Where(m => m.IdOsoba == id)
                                  .SingleOrDefaultAsync();
            if (osoba != null)
            {
                return PartialView(osoba);
            }
            else
            {
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", id);
                return NotFound($"Neispravan id osobe: {id}");
            }
        }
        /// <summary>
        /// Ažurira postojeću osobu na temelju podataka iz obrasca.
        /// </summary>
        /// <param name="osoba">Osoba s ažuriranim podacima.</param>
        /// <returns>Rezultat ažuriranja osobe.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit(Osoba osoba)
        {

            if (osoba == null)
            {
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", osoba.IdOsoba);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await _context.Osoba.AnyAsync(m => m.IdOsoba == osoba.IdOsoba);
            if (!checkId)
            {
                return NotFound($"Neispravan id mjesta: {osoba?.IdOsoba}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(osoba);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Osoba azurirana - Id: {osoba.IdOsoba}, Ime: {osoba.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Osoba {osoba.Ime} azurirana.");
                    return RedirectToAction(nameof(Get), new { id = osoba.IdOsoba });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(osoba);
                }
            }
            else
            {

                return PartialView(osoba);
            }
        }

        /// <summary>
        /// Briše određenu osobu.
        /// </summary>
        /// <param name="id">Identifikator osobe koja se briše.</param>
        /// <returns>Rezultat brisanja osobe.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var osoba = await _context.Osoba.FindAsync(id);
            if (osoba != null)
            {
                try
                {
                    string naziv = osoba.Ime;
                    _context.Remove(osoba);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Osoba izbrisana - Id: {id}, Ime: {osoba.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation($"Osoba {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"osoba {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja osobe: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja osobe: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"osoba sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// Uvozi podatke o osobama iz Excel datoteke.
        /// </summary>
        /// <param name="file">Excel datoteka s podacima o osobama.</param>
        /// <returns>Rezultat uvoza podataka.</returns>
        [HttpPost]
        public async Task<IActionResult> ImportOsobaExcel(IFormFile file)
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

                        List<Osoba> vrsteUlogaList = new List<Osoba>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new Osoba
                            {
                                IdOsoba = new Random().Next(),
                                Ime = worksheet.Cells[row, 1].Value?.ToString(),
                                Prezime = worksheet.Cells[row, 2].Value?.ToString(),
                                Telefon = worksheet.Cells[row, 4].Value?.ToString(),
                                Email = worksheet.Cells[row, 5].Value?.ToString(),
                                Iban = worksheet.Cells[row, 6].Value?.ToString()
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