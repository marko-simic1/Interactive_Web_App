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
    /// Controller za obavljanje operacija nad osobama i za prikaz Razor View-a vezanih uz osobe.
    /// </summary>
	public class OsobaController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly ILogger<OsobaController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor razreda OsobaController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>


        public OsobaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<OsobaController> logger)
        {
            this._context = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih osoba u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
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
        /// End point koji vraća sve sadržaje vezano uz osobu.
        /// </summary>
        /// <param name="id">Osoba koji se traži.</param>
        /// <returns>vraća prikaz sadržaja osobe. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


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
        /// End point na koje se šalje zahtjev za unošenjem nove osobe.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje osobe. </returns>



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Partner> vrsteLista = _context.Partner.AsNoTracking().ToList();
            List<Uloga> vrsteUloga = _context.Uloga.AsNoTracking().ToList();

            var viewModel = new OsobaViewModel
            {
                partner = vrsteLista,
                uloga = vrsteUloga
            };

            return View(viewModel);
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem nove osobe.
        /// </summary>
        /// <param name="viewModel">ViewModel u koji unosimo osobu.</param>
        /// <returns>Redirect na početnu stranicu osoba. </returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OsobaViewModel viewModel)
        {
            Osoba osoba = viewModel.Osoba;
            int ulogaId = viewModel.UlogaId;

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




        /*

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var osoba = _context.Osoba.AsNoTracking().Where(d => d.IdOsoba.Equals(id)).SingleOrDefault();
            if (osoba == null)
            {
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", id);
                return NotFound("Ne postoji osoba s oznakom: " + id);
            }
            else
            {
                return View(osoba);
            }
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id)
        {
            try
            {
                Osoba osoba = await _context.Osoba.Where(d => d.IdOsoba.Equals(id)).FirstOrDefaultAsync();

                if (osoba == null)
                {
                    return NotFound("Neispravna oznaka osobe: " + id);
                }

                if (await TryUpdateModelAsync<Osoba>(osoba, "",
                    d => d.Ime, d => d.Prezime, d => d.Email, d => d.Telefon, d => d.Iban
                ))
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData[Constants.Message] = "Osoba ažurirana.";
                        TempData[Constants.ErrorOccurred] = false;
                        TempData["updated"] = true;
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception exc)
                    {
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        return View(osoba);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "...");
                    return View(osoba);
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
        /// End point na koje se šalje zahtjev za formom za ažuriranje osobe.
        /// </summary>
        /// <param name="id">Id osobe kojeg želimo ažurirati.</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima osobe.</returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


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
        /// End point na koje se šalje zahtjev za ažuriranje osobe.
        /// </summary>
        /// <param name="osoba">Osoba koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


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
        /// End point na koje se šalje zahtjev za ažuriranje osobe.
        /// </summary>
        /// <param name="id"> Id osobe koja se ažurira</param>
        /// <returns>Redirect na master detail sljedeće osobe </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>


        [HttpGet]
        public async Task<IActionResult> Edit2(int id)
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
        /// End point na koje se šalje zahtjev za ažuriranje osobe.
        /// </summary>
        /// <param name="osoba">Osoba koji se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>

        [HttpPost]
        public async Task<IActionResult> Edit2(Osoba osoba)
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
                    return null;
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return null;
                }
            }
            else
            {

                return null;
            }
        }



        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var osoba = _context.Osoba.Find(id);

            if (osoba != null)
            {
                bool osobaUsed = false;
                if (_context.Partner.Any(p => p.IdPartnera == id)) osobaUsed = true;
                if (_context.Zadatak.Any(z => z.IdZadatka == id)) osobaUsed = true;
                if (_context.Posao.Any(p => p.IdPosla == id)) osobaUsed = true;
                if (_context.RadiNa.Any(p => p.IdProjekta == id)) osobaUsed = true;
                if (_context.RadiNa.Any(p => p.IdUloge == id)) osobaUsed = true;

                if (osobaUsed)
                {
                    TempData[Constants.Message] = "Osoba se koristi";
                    TempData[Constants.ErrorOccurred] = true;
                    TempData["used"] = true;
                }
                else
                {
                    try
                    {
                        string naziv = osoba.Ime;
                        _context.Remove(osoba);
                        _context.SaveChanges();
                        logger.LogInformation($"Osoba {naziv} uspješno obrisana");
                        TempData[Constants.Message] = $"Osoba {naziv} uspješno obrisana";
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
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", id);
                TempData[Constants.Message] = "Ne postoji osoba s oznakom: " + id;
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
        }
        */

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem osobe.
        /// </summary>
        /// <param name="id">Osoba koja se briše.</param>


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
        /// End point na koje se šalje zahtjev za brisanjem osobe.
        /// </summary>
        /// <param name="index">Osoba koji se briše</param>
        /// <returns>Redirect na istu stranicue </returns>


        [HttpDelete]
        public async Task<IActionResult> Delete2(int index)
        {

            var osobe = await _context.Osoba.OrderBy(z => z.IdOsoba).ToListAsync();
            int newid = osobe.First().IdOsoba;

            ActionResponseMessage responseMessage;
            var osoba = await _context.Osoba.FindAsync(index);
            if (osoba != null)
            {
                try
                {
                    string naziv = osoba.Ime;
                    _context.Remove(osoba);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Osoba izbrisana - Id: {index}, Ime: {osoba.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation($"Osoba {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"osoba {naziv} sa šifrom {index} uspješno obrisano.");

                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja osobe: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja osobe: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"osoba sa šifrom {index} ne postoji");
                logger.LogWarning("Ne postoji osoba s oznakom: {0} ", index);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });


            return Content("<script>window.location.href=/rppp/02/Osoba/index2/0;</script>");
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem osobe putem excela.
        /// </summary>
        /// <param name="file">Datoteka koju pregledavamo.</param>
        /// <returns>Novu datoteku koja označava dodane stavke. </returns>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>


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


        /// <summary>
        /// End point na koje se šalje zahtjev za prikaz master detail osobe.
        /// </summary>
        /// <param name="id">Osoba koja se prikazuje.</param>
        /// <returns>Redirect na master detail. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>


        [HttpGet]
        public async Task<IActionResult> index2(int id)
        {

            try
            {
                var osoba = await _context.Osoba
                .Where(z => z.IdOsoba == id)
                .Include(z => z.RadiNa)
                .ThenInclude(x => x.IdUlogeNavigation)
                .AsNoTracking()
                .SingleOrDefaultAsync();


                if (osoba == null)
                {
                    return NotFound();
                }

                OsobaViewModel model = new OsobaViewModel(osoba);

                return View(model);

            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }

        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail sljedeće osobe osobe.
        /// </summary>
        /// <param name="crntOsobaId">Osoba koja se trenutno prikatzuje</param>
        /// <returns>Redirect na master detail osobe </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>


        [HttpGet]
        public async Task<IActionResult> SljedecaOsoba(int crntOsobaId)
        {
            try
            {
                var crntOsoba = await _context.Osoba
                    .Where(z => z.IdOsoba == crntOsobaId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntOsoba == null)
                {
                    return NotFound("Current Osoba not found.");
                }


                var osobe = await _context.Osoba.OrderBy(z => z.IdOsoba).ToListAsync();

                int index = osobe.FindIndex(z => z.IdOsoba == crntOsobaId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int nextOsobaId = osobe[(index + 1) % osobe.Count()].IdOsoba;

                return RedirectToAction("index2", new { id = nextOsobaId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail prethodne osobe osobe.
        /// </summary>
        /// <param name="crntOsobaId">Osoba koja se trenutno prikatzuje</param>
        /// <returns>Redirect na master detail osobe </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>

        [HttpGet]
        public async Task<IActionResult> PrethodnaOsoba(int crntOsobaId)
        {
            try
            {
                var crntOsoba = await _context.Osoba
                    .Where(z => z.IdOsoba == crntOsobaId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntOsoba == null)
                {
                    return NotFound("Current Osoba not found.");
                }


                var osobe = await _context.Osoba.OrderBy(z => z.IdOsoba).ToListAsync();

                int index = osobe.FindIndex(z => z.IdOsoba == crntOsobaId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int newIndex = (index - 1) % osobe.Count();

                newIndex = newIndex < 0 ? osobe.Count() - 1 : newIndex;

                int prevZahtjevId = osobe[newIndex].IdOsoba;

                return RedirectToAction("index2", new { id = prevZahtjevId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }


    }
}
