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
using SkiaSharp;


namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Controller za obavljanje operacija nad poslovima i za prikaz Razor View-a vezanih uz poslove.
    /// </summary>
    public class PosaoController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<PosaoController> logger;
        private readonly AppSettings appSettings;

        /// <summary>
        /// Konstruktor razreda PosaoController.
        /// Prima dependency injection objekte potrebne za rad kontrolera.
        /// </summary>
        /// <param name="ctx">Dependency injected databse object. Služi za obavljanje operacija nad bazom.</param>
        /// <param name="options">ASP.NET Core's ugrađeni dependency injection i služi za ubacivanje konfiguraicija u kontroler.</param>
        /// <param name="logger">Dependency injected Logger object. SLuži za logiranje infromacija na konzolu koje služe programeru za pomoć.</param>
        
        public PosaoController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<PosaoController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
            appSettings = options.Value;
        }
        /// <summary>
        /// End point na koje se šalje zahtjev za prikazom svih poslova u bazi određenim redosljedom i straničenjem.
        /// </summary>
        /// <param name="page">Broj stranice.</param>
        /// <param name="sort">Broj propertija po kojem se sortira. (Naslov, opis...)</param>
        /// <param name="ascending">Uzlazno ili izlazno po propertyu koji sort predstavlja</param>
        /// <returns>Razor view sa svim podacima.</returns>

        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int pagesize = appSettings.PageSize;

            var query = ctx.Posao.AsNoTracking();

            int count = query.Count();
            if (count == 0)
            {
                TempData[Constants.Message] = "Ne postoji niti jedan posao.";
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

            var poslovi = await query
                          .Select(i => new PosloviViewModel
                          {
                              IdPosla = i.IdPosla,
                              ImePosla = i.ImePosla,
                              Opis = i.Opis,
                              IdVrste = i.IdVrste,
                              ImeVrste = i.ImePosla,

                          })
                          .Skip((page - 1) * pagesize)
                          .Take(pagesize)
                          .ToListAsync();

            var model = new PosaoViewModel
            {
                poslovi = poslovi,
                PagingInfo = pagingInfo
            };

            return View(model);
        }


        /// <summary>
        /// End point koji vraća sve sadržaje vezano uz posao.
        /// </summary>
        /// <param name="id">Posao koji se traži.</param>
        /// <returns>vraća prikaz sadržaja poslova. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var posao = await ctx.Posao
                                  .Where(m => m.IdPosla == id)
                                  .Select(m => new PosloviViewModel
                                  {
                                      IdPosla = m.IdPosla,
                                      ImePosla=m.ImePosla,
                                      Opis = m.Opis,
                                      

                                  })
                                  .SingleOrDefaultAsync();
            if (posao != null)
            {
                return PartialView(posao);
            }
            else
            {
                return NotFound($"Neispravan id posla: {id}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem novog posla.
        /// </summary>
        /// <returns>Vraća prikaz konfiguracije za stvaranje posla. </returns>
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Posao> poslovi = ctx.Posao.AsNoTracking().ToList();
            // List<Uloga> vrsteUloga = ctx.Uloga.AsNoTracking().ToList();

            var viewModel = new PosaoViewModel
            {
                Poslovi = poslovi
            };

            return View(viewModel);


        }

        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem novog posla.
        /// </summary>
        /// <param name="viewModel">ViewModel u koji unosimo posao.</param>
        /// <returns>Redirect na početnu stranicu poslova. </returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PosaoViewModel viewModel)
        {
            Posao posao = viewModel.Posao;


            logger.LogTrace(JsonSerializer.Serialize(posao));
            if (ModelState.IsValid)
            {
                try
                {
                    int id;
                    do
                    {
                        id = new Random().Next();
                        var query = ctx.Posao.Select(o => o.IdPosla).ToList();

                        if (!query.Contains(id))
                        {
                            break;
                        }

                    } while (true);
                    posao.IdPosla = id;
                    ctx.Add(posao);
                    await ctx.SaveChangesAsync();

                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Posao dodan - Id: {id}, Ime: {posao.ImePosla}",
                        Time = DateTime.Now
                    });

                    await ctx.SaveChangesAsync();
                    ctx.SaveChanges();
                    logger.LogInformation(new EventId(1000), $"Posao {posao.ImePosla} dodan.");
                    TempData["created"] = true;
                    TempData[Constants.Message] = $"Posao {posao.ImePosla} dodan.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanje novog posla: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    viewModel.Poslovi = ctx.Posao.AsNoTracking().ToList();
                    return View(viewModel);
                }
            }
            else
            {

                viewModel.Poslovi = ctx.Posao.AsNoTracking().ToList();
                return View(posao);
            }
        }





        //POST brisanje projekta
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var posao = await ctx.Posao.FindAsync(id);
            if (posao != null)
            {
                try
                {
                    ctx.Remove(posao);
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = $"Posao uspješno obrisan.";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch
                {
                    TempData[Constants.Message] = "Pogreška prilikom brisanja posla";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index), new
            {
                page,
                sort,
                ascending
            });
        }
        */
        /// <summary>
        /// End point na koje se šalje zahtjev za formom za ažuriranje posla.
        /// </summary>
        /// <param name="id">Id posla kojeg želimo ažurirati.</param>
        /// <returns>View s formom za ažuriranje i popunjenim podacima posla.</returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var posao = await ctx.Posao
                                  .AsNoTracking()
                                  .Where(i => i.IdPosla == id)
                                  .SingleOrDefaultAsync();
            if (posao != null)
            {
                /*
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                await PrepareDropDownLists();
                return View(posao)
                */
                return PartialView(posao);

            }
            else
            {
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", id);
                return NotFound($"Neispravan id posla: {id}");
            }
        }

        //POST forma za edit projekta
        /*
        [HttpPost, ActionName("Edit")]
        async Task<IActionResult> Editing(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            Posao posao = await ctx.Posao
                                  .Where(d => d.IdPosla == id)
                                  .FirstOrDefaultAsync();
            if (posao == null)
            {
                return NotFound();
            }
            bool checkId = await ctx.Posao.AnyAsync(i => i.IdPosla == posao.IdPosla);
            if (!checkId)
            {
                return NotFound($"Neispravan id projekta: {posao?.IdPosla}");
            }

            if (await TryUpdateModelAsync<Posao>(posao, "",
                    i => i.ImePosla, i => i.Opis, i => i.IdVrste))
            {
                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                try
                {
                    await ctx.SaveChangesAsync();
                    TempData[Constants.Message] = "Posao ažuriran.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index), new { page = page, sort = sort, ascending = ascending });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.Message);
                    return View(posao);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Podatke o poslu nije moguće povezati s forme");
                return View(posao);
            }
        }

        */

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje posla.
        /// </summary>
        /// <param name="posao">Posao koji se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>

        [HttpPost]
        public async Task<IActionResult> Edit(Posao posao)
        {

            if (posao == null)
            {
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", posao.IdPosla);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.Posao.AnyAsync(m => m.IdPosla == posao.IdPosla);
            if (!checkId)
            {
                return NotFound($"Neispravan id mjesta: {posao?.IdPosla}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(posao);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Posao azuriran - Id: {posao.IdPosla}, Ime: {posao.ImePosla}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Posao {posao.ImePosla} azuriran.");
                    return RedirectToAction(nameof(Get), new { id = posao.IdPosla });
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());

                    return PartialView(posao);
                }
            }
            else
            {

                return PartialView(posao);
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje posla.
        /// </summary>
        /// <param name="id"> Id posla koja se ažurira</param>
        /// <returns>Redirect na master detail sljedećeg posla </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>

        [HttpGet]
        public async Task<IActionResult> Edit2(int id)
        {
            var posao = await ctx.Posao
                                  .AsNoTracking()
                                  .Where(m => m.IdPosla == id)
                                  .SingleOrDefaultAsync();
            if (posao != null)
            {
                return PartialView(posao);
            }
            else
            {
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", id);
                return NotFound($"Neispravan id posla: {id}");
            }
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za ažuriranje posla.
        /// </summary>
        /// <param name="posao">Posao koji se ažurira.</param>
        /// <returns>Redirect na istu stranicu. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>

        [HttpPost]
        public async Task<IActionResult> Edit2(Posao posao)
        {

            if (posao == null)
            {
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", posao.IdPosla);
                return NotFound("Nema poslanih podataka");
            }
            bool checkId = await ctx.Posao.AnyAsync(m => m.IdPosla == posao.IdPosla);
            if (!checkId)
            {
                return NotFound($"Neispravan id : {posao?.IdPosla}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ctx.Update(posao);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Posao azuriran - Id: {posao.IdPosla}, Ime: {posao.ImePosla}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation(new EventId(1000), $"Posao {posao.ImePosla} azurirana.");
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
        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem posla.
        /// </summary>
        /// <param name="id">Posao koja se briše.</param>

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResponseMessage responseMessage;
            var posao = await ctx.Posao.FindAsync(id);
            if (posao != null)
            {
                try
                {
                    string naziv = posao.ImePosla;
                    ctx.Remove(posao);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Posao izbrisan - Id: {id}, Ime: {posao.ImePosla}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Posao {naziv} uspješno obrisan");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"Posao {naziv} sa šifrom {id} uspješno obrisano.");
                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja posla: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja posla: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Posao sa šifrom {id} ne postoji");
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", id);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
            return responseMessage.MessageType == MessageType.Success ?
              new EmptyResult() : await Get(id);
        }

        /// <summary>
        /// End point na koje se šalje zahtjev za brisanjem osobe.
        /// </summary>
        /// <param name="index">Posao koji se briše</param>
        /// <returns>Redirect na istu stranicue </returns>
        [HttpDelete]
        public async Task<IActionResult> Delete2(int index)
        {

            var poslovi = await ctx.Posao.OrderBy(z => z.IdPosla).ToListAsync();
            int newid = poslovi.First().IdPosla;

            ActionResponseMessage responseMessage;
            var posao = await ctx.Posao.FindAsync(index);
            if (posao != null)
            {
                try
                {
                    string naziv = posao.ImePosla;
                    ctx.Remove(posao);
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Posao izbrisan - Id: {index}, Ime: {posao.ImePosla}",
                        Time = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                    logger.LogInformation($"Posao {naziv} uspješno obrisan");
                    responseMessage = new ActionResponseMessage(MessageType.Success, $"posao {naziv} sa šifrom {index} uspješno obrisano.");

                }
                catch (Exception exc)
                {
                    responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja posla: {exc.CompleteExceptionMessage()}");
                    logger.LogError("Pogreška prilikom brisanja posla: " + exc.CompleteExceptionMessage());
                }
            }
            else
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"posao sa šifrom {index} ne postoji");
                logger.LogWarning("Ne postoji posao s oznakom: {0} ", index);
            }

            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });

            return Content("<script>window.location.href='/Posao/Index2/{newid}';</script>");
        }


        /// <summary>
        /// End point na koje se šalje zahtjev za unošenjem posla putem excela.
        /// </summary>
        /// <param name="file">Datoteka koju pregledavamo.</param>
        /// <returns>Novu datoteku koja označava dodane stavke. </returns>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>
        [HttpPost]
        public async Task<IActionResult> ImportPosaoExcel(IFormFile file)
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

                        List<Posao> vrsteUlogaList = new List<Posao>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {
                            var vrstaUloge = new Posao
                            {
                                IdPosla = new Random().Next(),
                                ImePosla = worksheet.Cells[row, 1].Value?.ToString(),
                                Opis = worksheet.Cells[row, 2].Value?.ToString(),

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

        /// <summary>
        /// End point na koje se šalje zahtjev za prikaz master detail posla.
        /// </summary>
        /// <param name="id">Posao koja se prikazuje.</param>
        /// <returns>Redirect na master detail. </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>

        [HttpGet]
        public async Task<IActionResult> Index2(int id)
        {
            try
            {
                var posao = await ctx.Posao
                .Where(z => z.IdPosla == id)
                .Include(z => z.IdOsoba)
                //.ThenInclude(x=> x.IdOsoba)

                .AsNoTracking()
                .SingleOrDefaultAsync();


                if (posao == null)
                {
                    return NotFound();
                }

                PosaoViewModel model = new PosaoViewModel(posao);

                return View(model);

            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }

        }

        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail sljedećeg posla.
        /// </summary>
        /// <param name="crntPosaoId">Posoa koja se trenutno prikazuje</param>
        /// <returns>Redirect na master detail posla </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>
        [HttpGet]
        public async Task<IActionResult> SljedeciPosao(int crntPosaoId)
        {
            try
            {
                var crntPosao = await ctx.Posao
                    .Where(z => z.IdPosla == crntPosaoId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntPosao == null)
                {
                    return NotFound("Current Posao not found.");
                }


                var poslovi = await ctx.Posao.OrderBy(z => z.IdPosla).ToListAsync();

                int index = poslovi.FindIndex(z => z.IdPosla == crntPosaoId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int nextPosaoId = poslovi[(index + 1) % poslovi.Count()].IdPosla;

                return RedirectToAction("Index2", new { id = nextPosaoId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }
        /// <summary>
        /// End point na koje se šalje zahtjev za prikazavinjem master detail prethodnog posla.
        /// </summary>
        /// <param name="crntPosaoId">Posao koja se trenutno prikatzuje</param>
        /// <returns>Redirect na master detail posla </returns>
        /// <response code="404">Not found ukoliko ne postoji zahtjev s tim id-om.</response>
        /// <response code="500">Prilikom greške pri upisivanju u bazu.</response>
        [HttpGet]
        public async Task<IActionResult> PrethodniPosao(int crntPosaoId)
        {
            try
            {
                var crntPosao = await ctx.Posao
                    .Where(z => z.IdPosla == crntPosaoId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (crntPosao == null)
                {
                    return NotFound("Current Posao not found.");
                }

                var poslovi = await ctx.Posao.OrderBy(z => z.IdPosla).ToListAsync();

                int index = poslovi.FindIndex(z => z.IdPosla == crntPosaoId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Projekt.");
                }

                int newIndex = (index - 1) % poslovi.Count();

                newIndex = newIndex < 0 ? poslovi.Count() - 1 : newIndex;

                int prevPosjId = poslovi[newIndex].IdPosla;

                return RedirectToAction("index2", new { id = prevPosjId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

    }
}