using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using OfficeOpenXml;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    public class ZadaciController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly ILogger<OsobaController> logger;
        private readonly AppSettings appSettings;
        public ZadaciController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<OsobaController> logger)
        {
            this.ctx = ctx;
            this.appSettings = options.Value;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ImportZadaci(IFormFile file)
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

                        List<Zadatak> ZadaciList = new List<Zadatak>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {

                            // Imena su tipa text pa ne mogu supoređivat text i varcahr

                            string zahtjevNaslov = worksheet.Cells[row, 8].Value?.ToString().Trim();
                            Zahtjev zahtjev = await ctx.Zahtjev
                                .Where(z => EF.Functions.Like(z.Naslov, zahtjevNaslov))  // Use EF.Functions.Like for comparison
                                .FirstOrDefaultAsync();

                            string imeStatusa = worksheet.Cells[row, 9].Value?.ToString().Trim();
                            Status status = await ctx.Status
                                .Where(s => EF.Functions.Like(s.ImeStatusa, imeStatusa))
                                .FirstOrDefaultAsync();


                            // Create a new Zahtjev entity
                            var zadatak = new Zadatak
                            {
                                Naslov = worksheet.Cells[row, 1].Value?.ToString(),
                                Opis = worksheet.Cells[row, 2].Value?.ToString(),
                                PlanPocetak = DateTime.Parse(worksheet.Cells[row, 3].Value?.ToString()),
                                PlanKraj = DateTime.Parse(worksheet.Cells[row, 4].Value?.ToString()),
                                Prioritet = worksheet.Cells[row, 7].Value?.ToString(),
                            };

                            if (worksheet.Cells[row, 5].Value?.ToString().Trim() != "Nije još započeo")
                            {
                                zadatak.StvPoc = DateTime.Parse(worksheet.Cells[row, 5].Value?.ToString());
                            }

                            if (worksheet.Cells[row, 6].Value?.ToString().Trim() != "Nije još završio")
                            {
                                zadatak.StvKraj = DateTime.Parse(worksheet.Cells[row, 6].Value?.ToString());
                            }

                            if (zahtjev != null)
                            {
                                zadatak.IdZahtjevaNavigation = zahtjev;
                                zadatak.IdZahtjeva = zahtjev.IdZahtjeva;
                            }

                            if (status != null)
                            {
                                zadatak.IdStatusaNavigation = status;
                                zadatak.IdStatusa = status.IdStatusa;
                            }

                            ZadaciList.Add(zadatak);
                        }

                        ctx.AddRange(ZadaciList);
                        await ctx.SaveChangesAsync();

                        var newPackage = new ExcelPackage();
                        var newWorksheet = newPackage.Workbook.Worksheets.Add("Sheet1");

                        newWorksheet.Cells[1, 1].Value = "Naslov";
                        newWorksheet.Cells[1, 2].Value = "Opis";
                        newWorksheet.Cells[1, 3].Value = "Planirani Početak";
                        newWorksheet.Cells[1, 4].Value = "Planirani Kraj";
                        newWorksheet.Cells[1, 5].Value = "Stvarni Početak";
                        newWorksheet.Cells[1, 6].Value = "Stvarni Kraj";
                        newWorksheet.Cells[1, 7].Value = "Prioritet";
                        newWorksheet.Cells[1, 8].Value = "Zahtjev";
                        newWorksheet.Cells[1, 9].Value = "Status";

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


        #region Zadaci-create, edit, detele, fetch all forms and data
        [HttpGet]
        public IActionResult Index(int page = 1, int sort = 1, bool ascending = true)
        {
            //var zadaci = await ctx.Zadatak.Include(x => x.IdOsobaNavigation).Include(x => x.IdStatusaNavigation).Include(x =>
            //x.IdZahtjevaNavigation).ToListAsync();

            //return View(new ZadaciViewModel(zadaci));

            int pagesize = appSettings.PageSize;
            var query = ctx.Zadatak.Include(x => x.IdOsobaNavigation).Include(x => x.IdStatusaNavigation).Include(x =>
            x.IdZahtjevaNavigation).AsNoTracking();

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

            var zadaci = query.Skip((page - 1) * pagesize).Take(pagesize).ToList();

            var model = new ZadaciViewModel
            {
                Zadaci = zadaci,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteZadatak(int id)
        {
            var zadatak = await ctx.Zadatak.FindAsync(id);

            if (zadatak == null)
            {
                TempData["NotFoundMessage"] = $"Zadatak ne postoji";
                return NotFound(); // Or any other appropriate status code
            }

            try
            {
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Obrisan zadatak - Id: {zadatak.IdZadatka}, Naslov zadatka: {zadatak.Naslov}",
                    Time = DateTime.Now
                });
                ctx.Remove(zadatak);
                await ctx.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Zadatak {zadatak.Naslov} uspješno obrisan";


                var callingUrl = HttpContext.Request.Headers["Referer"].ToString();
                string[] routeParams = callingUrl.Split('/');

                if (routeParams[routeParams.Length - 2] == "ZahtjevMD")
                {
                    return RedirectToAction("ZahtjevMD", "Zahtjev", new { id = routeParams[routeParams.Length - 1] });
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting the task: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

        }

        [HttpGet]
        public async Task<IActionResult> EditFormZadatak(int id, int sort, int page, bool ascending)
        {
            var zadatak = await ctx.Zadatak.FindAsync(id);

            if (zadatak == null)
            {
                TempData["NotFoundMessage"] = $"Zadatak ne postoji";
                return NotFound(); // Or any other appropriate status code
            }

            var zahtjevi = await ctx.Zahtjev.ToListAsync();
            var osobe = await ctx.Osoba.ToListAsync();
            var statusi = await ctx.Status.ToListAsync();

            ZadaciViewModel model = new ZadaciViewModel(zadatak, zahtjevi, osobe, statusi);
            model.PagingInfo = new PagingInfo { Ascending = ascending, CurrentPage = page, Sort = sort };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditZadatak(int IdZahtjeva, string Naslov, string Opis, string PlanPoc, string PlanKraj, string? StvPoc, string? StvKraj, string Prioritet, int Zahtjev, int Status, int Osoba, string callingUrl, int id, int sort, int page, bool ascending)
        {
            try
            {

                var existingZadatak = await ctx.Zadatak.FindAsync(IdZahtjeva);

                if (existingZadatak == null)
                {
                    TempData["ErrorMessage"] = $"Zadatak {Naslov} ne postoji.";
                    return RedirectToAction(nameof(Index));
                }

                DateTime? parsedStvPoc = string.IsNullOrEmpty(StvPoc) ? (DateTime?)null : DateTime.Parse(StvPoc);
                DateTime? parsedStvKraj = string.IsNullOrEmpty(StvKraj) ? (DateTime?)null : DateTime.Parse(StvKraj);

                if (DateTime.Parse(PlanPoc) > DateTime.Parse(PlanKraj))
                {
                    TempData["ErrorMessage"] = $"Kraj ne može biti prije početka";
                    return RedirectToAction("CreateFormZadatak");
                }

                if (parsedStvPoc != null && parsedStvKraj != null && parsedStvPoc > parsedStvKraj)
                {
                    TempData["ErrorMessage"] = $"Kraj ne može biti prije početka";
                    return RedirectToAction("CreateFormZadatak");
                }


                existingZadatak.Naslov = Naslov;
                existingZadatak.Opis = Opis;
                existingZadatak.PlanPocetak = DateTime.Parse(PlanPoc);
                existingZadatak.PlanKraj = DateTime.Parse(PlanKraj);
                existingZadatak.StvPoc = parsedStvPoc;
                existingZadatak.StvKraj = parsedStvKraj;
                existingZadatak.Prioritet = Prioritet;
                existingZadatak.IdZahtjeva = Zahtjev;
                existingZadatak.IdStatusa = Status;
                existingZadatak.IdOsoba = Osoba;

                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Ažuriran zadatak - Id: {existingZadatak.IdZadatka}, Naslov zadatka: {existingZadatak.Naslov}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zadatak uspješno ažuriran";


                string[] routeParams = callingUrl.Split('/');

                if (routeParams[routeParams.Length - 2] == "ZahtjevMD")
                {
                    return RedirectToAction("ZahtjevMD", "Zahtjev", new { id = routeParams[routeParams.Length - 1] });

                }
                else
                {
                    return RedirectToAction("Index", new { page = page, sort = sort, ascending = ascending });
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error editing the task: {ex.Message}";
                return RedirectToAction("EditFormZadatak", new { id = IdZahtjeva });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateFormZadatak(int sort, int page, bool ascending, int? id = null)
        {
            var zahtjevi = await ctx.Zahtjev.ToListAsync();
            var osobe = await ctx.Osoba.ToListAsync();
            var statusi = await ctx.Status.ToListAsync();

            ZadaciViewModel model = new ZadaciViewModel(id, null, zahtjevi, osobe, statusi);
            model.PagingInfo = new PagingInfo { Ascending = ascending, CurrentPage = page, Sort = sort };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateZadatak(string Naslov, string Opis, string PlanPoc, string PlanKraj, string? StvPoc, string? StvKraj, string Prioritet, int ZahtjevId, int Status, int Osoba, string callingUrl, int sort, int page, bool ascending)
        {
            try
            {
                DateTime? parsedStvPoc = string.IsNullOrEmpty(StvPoc) ? (DateTime?)null : DateTime.Parse(StvPoc);
                DateTime? parsedStvKraj = string.IsNullOrEmpty(StvKraj) ? (DateTime?)null : DateTime.Parse(StvKraj);

                if (DateTime.Parse(PlanPoc) > DateTime.Parse(PlanKraj))
                {
                    TempData["ErrorMessage"] = $"Kraj ne može biti prije početka";
                    return RedirectToAction("CreateFormZadatak");
                }

                if (parsedStvPoc != null && parsedStvKraj != null && parsedStvPoc > parsedStvKraj)
                {
                    TempData["ErrorMessage"] = $"Kraj ne može biti prije početka";
                    return RedirectToAction("CreateFormZadatak");
                }


                Zadatak zadatak = new Zadatak
                {
                    Naslov = Naslov,
                    Opis = Opis,
                    PlanPocetak = DateTime.Parse(PlanPoc),
                    PlanKraj = DateTime.Parse(PlanKraj),
                    StvPoc = parsedStvPoc,
                    StvKraj = parsedStvKraj,
                    Prioritet = Prioritet,
                    IdZahtjeva = ZahtjevId,
                    IdStatusa = Status,
                    IdOsoba = Osoba
                };

                ctx.Add(zadatak);
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Dodan novi zadatak - Id: {zadatak.IdZadatka}, Naslov zadatka: {zadatak.Naslov}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zadatak uspješno kreiran";

                string[] routeParams = callingUrl.Split('/');

                if (routeParams[routeParams.Length - 2] == "ZahtjevMD")
                {
                    return RedirectToAction("ZahtjevMD", "Zahtjev", new { id = routeParams[routeParams.Length - 1] });
                }
                else
                {
                    return RedirectToAction("Index", new { sort = sort, ascending = ascending, page = page });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating the task: {ex.Message}";
                return RedirectToAction("CreateFormZadatak");
            }
        }
        #endregion


        #region Statusi-create, edit, detele, fetch all forms and data
        [HttpGet]
        public IActionResult Status(int page = 1, int sort = 1, bool ascending = true)
        {
            //var statusi = await ctx.Status.Include(x => x.Zadatak).ToListAsync();

            //return View(statusi);
            int pagesize = appSettings.PageSize;
            var query = ctx.Status.Include(x => x.Zadatak).AsNoTracking();

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
                return RedirectToAction(nameof(Status), new { page = 1, sort, ascending });
            }

            query = query.ApplySort(sort, ascending);

            var statusi = query.Skip((page - 1) * pagesize).Take(pagesize).ToList();

            var model = new ZadaciViewModel
            {
                Statusi = statusi,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStatus(string ImeStatusa)
        {
            try
            {
                Status status = new Status
                {
                    ImeStatusa = ImeStatusa
                };

                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Dodan novi status - Id: {status.IdStatusa}, Ime statusa: {status.ImeStatusa}",
                    Time = DateTime.Now
                });
                ctx.Add(status);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Status uspješno kreiran";
                return RedirectToAction("Status");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška kod stvaranja statusa";
                return RedirectToAction("Status");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var status = await ctx.Status.FindAsync(id);

            if (status == null)
            {
                TempData["NotFoundMessage"] = $"Status ne postoji";
                return NotFound(); // Or any other appropriate status code
            }

            try
            {
                ctx.Remove(status);
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Izbrisan status - Id: {status.IdStatusa}, Ime statusa: {status.ImeStatusa}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Status {status.ImeStatusa} uspješno obrisan";
                return RedirectToAction(nameof(Status));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting the status: {ex.Message}";
                return RedirectToAction(nameof(Status));
            }

        }

        [HttpGet]
        public async Task<IActionResult> EditFormStatus(int id)
        {
            var status = await ctx.Status.FindAsync(id);

            if (status == null)
            {
                TempData["NotFoundMessage"] = $"Status ne postoji";
                return NotFound(); // Or any other appropriate status code
            }

            return View(status);
        }

        [HttpPost]
        public async Task<IActionResult> EditStatus(int IdVrsteStatusa, string Ime)
        {
            try
            {

                var existingStatus = await ctx.Status.FindAsync(IdVrsteStatusa);

                if (existingStatus == null)
                {
                    TempData["ErrorMessage"] = $"Status {Ime} ne postoji.";
                    return RedirectToAction(nameof(Status));
                }

                existingStatus.ImeStatusa = Ime;

                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Ažuriran status - Id: {existingStatus.IdStatusa}, Ime statusa: {existingStatus.ImeStatusa}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Status uspješno ažuriran";
                //return RedirectToAction("EditFormStatus", new { id = IdVrsteStatusa });
                return RedirectToAction("Status", new { page = 1, sort = 1, ascending = true });

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error editing the status: {ex.Message}";
                return RedirectToAction("EditFormStatus", new { id = IdVrsteStatusa });
            }
        }
        #endregion
    }
}