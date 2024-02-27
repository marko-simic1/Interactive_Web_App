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
    /// Kontroler za upravljanje partnerima.
    /// </summary>

    public class PartnerController : Controller
    {
        private readonly ProjektDbContext _context;
        private readonly ILogger<PartnerController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor PartnerController-a.
        /// </summary>
        /// <param name="ctx">Kontekst baze podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zabilježavanje informacija.</param>

        public PartnerController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<PartnerController> logger)
        {
            this._context = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }

        /// <summary>
        /// Prikazuje popis partnera.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Vrsta sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s popisom partnera.</returns>

        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = _context.Partner.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedan partner");
                TempData[Constants.Message] = "Ne postoji nijedan partner.";
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

            var partneri = await query
                          .Select(m => new PartneriViewModel
                          {
                              IdPartnera = m.IdPartnera,
                              Ime = m.Ime,
                              Oib = m.Oib,
                              Email = m.Email,
                              Iban = m.Iban,
                              Telefon = m.Telefon,
                              Adresa = m.Adresa,
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new PartnerViewModel
            {
                partneri = partneri,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Dohvaća informacije o određenom partneru.
        /// </summary>
        /// <param name="id">Identifikator partnera.</param>
        /// <returns>PartialView s informacijama o partneru ili NotFound ako partner ne postoji.</returns>


        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var partneri = await _context.Partner
                                  .Where(m => m.IdPartnera == id)
                                  .Select(m => new PartneriViewModel
                                  {
                                      IdPartnera = m.IdPartnera,
                                      Ime = m.Ime,
                                      Email = m.Email,
                                      Iban = m.Iban,
                                      Oib = m.Oib,
                                      Telefon = m.Telefon,
                                      Adresa = m.Adresa
                                  })
                                  .SingleOrDefaultAsync();
            if (partneri != null)
            {
                return PartialView(partneri);
            }
            else
            {
                return NotFound($"Neispravan id partnera: {id}");
            }
        }



        /// <summary>
        /// Prikazuje formu za stvaranje novog partnera.
        /// </summary>
        /// <returns>View za stvaranje novog partnera.</returns>



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Projekt> vrsteLista = _context.Projekt.AsNoTracking().ToList();

            var viewModel = new PartnerViewModel
            {
                projekti = vrsteLista
            };

            return View(viewModel);
        }

        /// <summary>
        /// Stvara novog partnera na temelju podataka iz forme.
        /// </summary>
        /// <param name="viewModel">ViewModel s podacima o partneru.</param>
        /// <returns>Redirect na Index ili View ako podaci nisu ispravni.</returns>


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartnerViewModel viewModel)
        {
            Partner partner = viewModel.Partner;

            logger.LogTrace(JsonSerializer.Serialize(partner));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(partner);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Partner dodan - Id: {partner.IdPartnera}, Ime: {partner.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    _context.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"Partner {partner.Ime} dodana.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"Partner {partner.Ime} dodana.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje novog partnera: {0}", exc.CompleteExceptionMessage());
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
        /// Prikazuje formu za uređivanje partnera.
        /// </summary>
        /// <param name="id">Identifikator partnera.</param>
        /// <returns>PartialView s podacima o partneru ili NotFound ako partner ne postoji.</returns>


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var partner = await _context.Partner
                                  .AsNoTracking()
                                  .Where(m => m.IdPartnera == id)
                                  .SingleOrDefaultAsync();
            if (partner != null)
            {
                return PartialView(partner);
            }
            else
            {
                logger.LogWarning("Ne postoji partner s oznakom: {0} ", id);
                return NotFound($"Neispravan id partnera: {id}");
            }
        }

        /// <summary>
        /// Ažurira podatke o partneru na temelju podataka iz forme.
        /// </summary>
        /// <param name="partner">Podaci o partneru za ažuriranje.</param>
        /// <returns>PartialView s podacima o partneru ili BadRequest ako podaci nisu ispravni.</returns>


        [HttpPost]
        public async Task<IActionResult> Edit(Partner partner)
        {

            if (partner == null)
            {
                logger.LogWarning("Ne postoji partner s oznakom: {0} ", partner.IdPartnera);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await _context.Partner.AnyAsync(m => m.IdPartnera == partner.IdPartnera);
            if (!checkId)
            {
                return NotFound($"Neispravan id partnera: {partner?.IdPartnera}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(partner);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Partner azuriran - Id: {partner.IdPartnera}, Ime: {partner.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Partner {partner.Ime} azuriran.");
                    return RedirectToAction(nameof(Get), new { id = partner.IdPartnera });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(partner);
                }
            }
            else
            {

                return PartialView(partner);
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
        /// Briše partnera na temelju identifikatora.
        /// </summary>
        /// <param name="id">Identifikator partnera za brisanje.</param>
        /// <returns>ActionResult koji uključuje informacije o rezultatu brisanja.</returns>


        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var partner = await _context.Partner.FindAsync(id);
            if (partner != null)
            {
                try
                {
                    string naziv = partner.Ime;
                    _context.Remove(partner);
                    _context.LogEntries.Add(new LogEntry
                    {
                        Action = $"Partner obrisan - Id: {id}, Ime: {partner.Ime}",
                        Time = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    logger.LogInformation($"Partner {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"partner {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja partnera: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja partnera: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"partner sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji partner s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// Uvozi podatke o partnerima iz Excel datoteke.
        /// </summary>
        /// <param name="file">Excel datoteka s podacima o partnerima.</param>
        /// <returns>ActionResult s rezultatom uvoza podataka.</returns>

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

                        List<Partner> vrsteUlogaList = new List<Partner>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new Partner
                            {
                                Ime = worksheet.Cells[row, 1].Value?.ToString(),
                                Oib = Convert.ToInt32(worksheet.Cells[row, 2].Value),
                                Telefon = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                Email = worksheet.Cells[row, 4].Value?.ToString(),
                                Iban = worksheet.Cells[row, 5].Value?.ToString(),
                                Adresa = worksheet.Cells[row, 6].Value?.ToString(),
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
