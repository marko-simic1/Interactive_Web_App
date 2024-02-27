using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
    /// Controller za obavljanje operacija nad vrstama projekta i za prikaz Razor View-a vezanih uz vrste projekata.
    /// </summary>
    public class VrstaProjektaController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<VrstaProjektaController> logger;
        private readonly AppSettings appSettings;


        /// <summary>
        /// Konstruktor razreda VrstaProjektaController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>
        public VrstaProjektaController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<VrstaProjektaController> logger)
        {
            this.ctx = ctx;
            this.appSettings = options.Value;
            this.logger = logger;
        }


        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih vrsta projekta u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.VrstaProjekta.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                logger.LogInformation("Ne postoji nijedna vrsta projekta");
                TempData[Constants.Message] = "Ne postoji niti jedna vrsta projekta.";
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

            var vrProj = await query
                          .Select(v => new VrstaProjektaViewModel
                          {
                              IdVrsteProjekta = v.IdVrste,
                              NazivVrsteProjekta = v.ImeVrste
                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new VrsteProjekataViewModel
            {
                VrsteProjekta = vrProj,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz vrstu projekta.
        /// </summary>
        /// <param name="id">Id vrste projekta koji se traži.</param>
        /// <returns>vraća prikaz sadržaja vrste projekta. </returns>
        /// <response code="404">Not found ukoliko ne postoji projekta s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var vrstaProj = await ctx.VrstaProjekta
                                  .Where(m => m.IdVrste == id)
                                  .Select(m => new VrstaProjektaViewModel
                                  {
                                      IdVrsteProjekta = m.IdVrste,
                                      NazivVrsteProjekta = m.ImeVrste

                                  })
                                  .SingleOrDefaultAsync();
            if (vrstaProj != null)
            {
                return PartialView(vrstaProj);
            }
            else
            {
                return NotFound($"Neispravan id vrste projekta: {id}");
            }
        }

        /// <summary>
        /// End point koji vraća formu za kreiranje vrste projekta.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje vrste projekta. </returns>
        [HttpGet]
        public Task<IActionResult> Create()
        {
            return Task.FromResult<IActionResult>(View());
        }


        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem nove vrste projekta.
        /// </summary>
        /// <param name="vrProj">ViewModel u koji unosimo vrstu projekta.</param>
        /// <returns>Redirect na početnu stranicu vrste projekta. </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VrstaProjekta vrProj)
        {
            logger.LogTrace(JsonSerializer.Serialize(vrProj));
            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Add(vrProj);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta projekta dodana - Id: {vrProj.IdVrste}, Ime: {vrProj.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    ctx.SaveChanges();
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"Vrsta projekta {vrProj.ImeVrste} dodanana. Id vrste projekta = {vrProj.IdVrste}";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(vrProj);
                }
            }
            else
            {
                return View(vrProj);
            }
        }



        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem vrste projekta.
        /// </summary>
        /// <param name="id">Id vrste projekta koja se briše.</param>
        [HttpDelete]

        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var vrsta = await ctx.VrstaProjekta.FindAsync(id);
            if (vrsta != null)
            {
                try
                {
                    ctx.Remove(vrsta);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta projekta obrisana - Id: {id}, Ime: {vrsta.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Vrsta projekta {vrsta.ImeVrste} uspješno obrisana");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"vrsta projekta {vrsta.ImeVrste} sa šifrom {id} uspješno obrisano.");
                    TempData[Constants.Message] = $"Vrsta projekta {vrsta.ImeVrste} uspješno obrisana.";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja vrste projekta: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja vrste projekta: " + exc.CompleteExceptionMessage());
                    TempData[Constants.Message] = "Pogreška prilikom brisanja vrste projekta";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"vrsta projekta sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji vrsta projekta s oznakom: {0} ", id);
            }
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// End point na koje se šalje id za formom za ažuriranje vrste projekta.
        /// </summary>
        /// <param name="id">Id vrste projekta kojeg želimo ažurirati.</param>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima vrste dokumentacije.</returns>
        /// <response code="404">Not found ukoliko ne postoji vrsta projekta s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var vrstaProj = await ctx.VrstaProjekta
                                  .AsNoTracking()
                                  .Where(i => i.IdVrste == id)
                                  .SingleOrDefaultAsync();
            if (vrstaProj != null)
            {
                return PartialView(vrstaProj);
            }
            else
            {
                logger.LogWarning("Ne postoji vrsta projekta s oznakom: {0} ", id);
                return NotFound();
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje vrste projekta.
        /// </summary>
        /// <param name="id">Id vrste projekta koja se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji vrsta projekta s tim id-om.</response>
        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> Editing(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            VrstaProjekta vrstaProj = await ctx.VrstaProjekta
                                  .Where(d => d.IdVrste == id)
                                  .FirstOrDefaultAsync();
            if (vrstaProj == null)
            {
                logger.LogWarning("Ne postoji vrsta projekta s oznakom: {0} ", id);
                return NotFound();
            }
            bool checkId = await ctx.VrstaProjekta.AnyAsync(i => i.IdVrste == vrstaProj.IdVrste);
            if (!checkId)
            {
                return NotFound($"Neispravan id vrste: {vrstaProj?.IdVrste}");
            }

            if (await TryUpdateModelAsync<VrstaProjekta>(vrstaProj, "",
                    i => i.ImeVrste))
            {
                try
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Vrsta projekta azurirana - Id: {vrstaProj.IdVrste}, Ime: {vrstaProj.ImeVrste}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = "Vrsta projekta ažurirana.";
                    TempData[Constants.ErrorOccurred] = false;
                    logger.LogInformation(new EventId(1000), $"Vrsta projekta {vrstaProj.ImeVrste} azurirana.");
                    return RedirectToAction(nameof(Get), new { id = vrstaProj.IdVrste });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(vrstaProj);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Podatke o vrsti projekta nije moguće povezati s forme");
                return View(vrstaProj);
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

                        List<VrstaProjekta> vrsteProjektaList = new List<VrstaProjekta>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaProjekta = new VrstaProjekta
                            {
                                ImeVrste = worksheet.Cells[row, 1].Value?.ToString()
                            };

                            vrsteProjektaList.Add(vrstaProjekta);
                        }

                        ctx.AddRange(vrsteProjektaList);
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
