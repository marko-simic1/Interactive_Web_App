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
    /// Kontroler za upravljanje vrstom poslova.
    /// </summary>
    public class VrstaPoslaController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<VrstaUlogeController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor VrstaPoslaController-a.
        /// </summary>
        /// <param name="ctx">Kontekst baze podataka.</param>
        /// <param name="options">Postavke aplikacije.</param>
        /// <param name="logger">Logger za zabilježavanje informacija.</param>
        public VrstaPoslaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<VrstaUlogeController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }
        /// <summary>
        /// Prikazuje popis vrste poslova.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Vrsta sortiranja.</param>
        /// <param name="ascending">Redoslijed sortiranja.</param>
        /// <returns>View s popisom vrste poslova.</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;
            var query = ctx.VrstaPosla.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedna vrsta posla");
                TempData[Constants.Message] = "Ne postoji nijedna vrsta posla.";
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

            var vrstePoslova = await query
                          .Select(m => new VrstePoslovaViewModel
                          {
                              IdVrstePosla = m.IdVrste,
                              NazivVrstePosla = m.ImeVrste
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new VrstaPoslaViewModel
            {
                vrste = vrstePoslova,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// Dohvaća informacije o određenoj vrsti posla.
        /// </summary>
        /// <param name="id">Identifikator vrste posla.</param>
        /// <returns>PartialView s informacijama o vrsti posla ili NotFound ako vrsta posla ne postoji.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var vrsta = await ctx.VrstaPosla
                                  .Where(m => m.IdVrste == id)
                                  .Select(m => new VrstePoslovaViewModel
                                  {
                                      IdVrstePosla = m.IdVrste,
                                      NazivVrstePosla = m.ImeVrste
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
        /// Prikazuje formu za stvaranje nove vrste posla.
        /// </summary>
        /// <returns>View za stvaranje nove vrste posla.</returns>


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        /// <summary>
        /// Stvara nove vrsta posla na temelju podataka iz forme.
        /// </summary>
        /// <param name="vrsta">ViewModel s podacima o vrsti posla.</param>
        /// <returns>Redirect na Index ili View ako podaci nisu ispravni.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VrstaPosla vrsta)
        {

            logger.LogTrace(JsonSerializer.Serialize(vrsta));

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Add(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta posla stvorena - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    ctx.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"VrstaPosla {vrsta.ImeVrste} dodana.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"VrstaPosla {vrsta.ImeVrste} dodana.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje nove vrste posla: {0}", exc.CompleteExceptionMessage());
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
        /// Prikazuje formu za uređivanje vrste posla.
        /// </summary>
        /// <param name="id">Identifikator vrste posla.</param>
        /// <returns>PartialView s podacima o vrsti posla ili NotFound ako vrsta posla ne postoji.</returns>

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vrsta = await ctx.VrstaPosla
                                  .AsNoTracking()
                                  .Where(m => m.IdVrste == id)
                                  .SingleOrDefaultAsync();
            if (vrsta != null)
            {
                return PartialView(vrsta);
            }
            else
            {
                logger.LogWarning("Ne postoji vrsta posla s oznakom: {0} ", id);
                return NotFound($"Neispravan id vrste: {id}");
            }
        }

        /// <summary>
        /// Ažurira podatke o vrsti posla na temelju podataka iz forme.
        /// </summary>
        /// <param name="vrsta">Podaci o vrsti posla za ažuriranje.</param>
        /// <returns>PartialView s podacima o vrsti posla ili BadRequest ako podaci nisu ispravni.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit(VrstaPosla vrsta)
        {

            if (vrsta == null)
            {
                logger.LogWarning("Ne postoji vrsta posla s oznakom: {0} ", vrsta.IdVrste);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.VrstaPosla.AnyAsync(m => m.IdVrste == vrsta.IdVrste);
            if (!checkId)
            {
                return NotFound($"Neispravan id vrste: {vrsta?.IdVrste}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta posla azurirana - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
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
        /// Briše vrstu posla na temelju identifikatora.
        /// </summary>
        /// <param name="id">Identifikator vrste posla za brisanje.</param>
        /// <returns>ActionResult koji uključuje informacije o rezultatu brisanja.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await ctx.VrstaPosla.FindAsync(id);
            if (vrsta != null)
            {
                try
                {
                    string naziv = vrsta.ImeVrste;
                    ctx.Remove(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta posla izbrisana - Id: {vrsta.IdVrste}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"vrsta posla {naziv} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"vrsta {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja vrste posla: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja vrste posla: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"vrsta sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji vrsta posla s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }
        /// <summary>
        /// Uvozi podatke o vrsta posla iz Excel datoteke.
        /// </summary>
        /// <param name="file">Excel datoteka s podacima o vrsti poslova.</param>
        /// <returns>ActionResult s rezultatom uvoza podataka.</returns>
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

                        List<VrstaPosla> vrstePoslaList = new List<VrstaPosla>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaPosla = new VrstaPosla
                            {
                                ImeVrste = worksheet.Cells[row, 1].Value?.ToString(),
                                IdVrste = Convert.ToInt32(worksheet.Cells[row, 2].Value),
                            };

                            vrstePoslaList.Add(vrstaPosla);
                        }

                        ctx.AddRange(vrstePoslaList);
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
