using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using OfficeOpenXml;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using SkiaSharp;

namespace RPPP_WebApp.Controllers
{
    public class ZahtjevController : Controller
    {

        private readonly ProjektDbContext ctx;
        private readonly AppSettings appSettings;
        private readonly ILogger<OsobaController> logger;

        public ZahtjevController(ProjektDbContext ctx, IOptionsSnapshot<AppSettings> options, ILogger<OsobaController> logger)
        {
            this.ctx = ctx;
            this.appSettings = options.Value;
            this.logger = logger;
        }

        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    var query = ctx.Zahtjev.AsNoTracking(); // AsNoTracking ne prati promjene nad podacima
        //    ZahtjevViewModel model = new ZahtjevViewModel(await query.ToListAsync());
        //    return View(model);
        //}

        [HttpGet]
        public IActionResult Zahtjevi(int page = 1, int sort = 1, bool ascending = true)
        {
            //// .include ce ucitati i podatke iz drugih tablica (što je vjv spječeno lazy loadingom koji EF koristi da oprimizira query)
            //var query = ctx.Zahtjev
            //                        .Include(z => z.IdProjektaNavigation)
            //                        .Include(z => z.IdVrsteNavigation)
            //                        .Include(z => z.Zadatak)
            //                        .AsNoTracking(); // AsNoTracking ne prati promjene nad podacima
            //ZahtjevViewModel model = new ZahtjevViewModel(await query.ToListAsync());
            //return View(model);


            int pagesize = appSettings.PageSize;


            var query = ctx.Zahtjev
                                    .Include(z => z.IdProjektaNavigation)
                                    .Include(z => z.IdVrsteNavigation)
                                    .Include(z => z.Zadatak)
                                    .AsNoTracking(); // AsNoTracking ne prati promjene nad podacima

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
                return RedirectToAction(nameof(Zahtjevi), new { page = 1, sort, ascending });
            }

            query = query.ApplySort(sort, ascending);

            var zahtjevi = query.Skip((page - 1) * pagesize).Take(pagesize).ToList();

            var model = new ZahtjevViewModel
            {
                Zahtjevi = zahtjevi,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> FormUpdateZahtjev(int id, int CurrentPage, int Sort, bool Ascending)
        {
            var query = await ctx.Zahtjev
                            .Where(z => z.IdZahtjeva == id)
                            .Include(z => z.IdProjektaNavigation)
                            .Include(z => z.IdVrsteNavigation)
                            .AsNoTracking()
                            .SingleOrDefaultAsync();

            if (query == null)
            {
                // Handle the case where no Zahtjev with the given ID is found
                return NotFound();
            }

            var query2 = await ctx.Projekt
                            .AsNoTracking()
                            .ToListAsync();

            var query3 = await ctx.VrstaZah
                            .AsNoTracking()
                            .ToListAsync();

            var viewModel = new ZahtjevViewModel(query, query2, query3);
            viewModel.PagingInfo = new PagingInfo { Ascending = Ascending, CurrentPage = CurrentPage, Sort = Sort };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateZahtjev(int IdZahtjeva, string Naslov, string Opis, int? Projekt, int? Vrsta, int? page, int? sort, bool? ascending)
        {

            try
            {
                // Retrieve the existing Zahtjev entity from the database using its ID
                var existingZahtjev = await ctx.Zahtjev.FindAsync(IdZahtjeva);

                if (existingZahtjev == null)
                {
                    return NotFound(); // Handle the case where the Zahtjev is not found
                }

                // Update the properties of the existing Zahtjev entity with new values
                existingZahtjev.Naslov = Naslov;
                existingZahtjev.Opis = Opis;
                existingZahtjev.IdProjekta = Projekt;
                existingZahtjev.IdVrste = Vrsta;

                // Save changes back to the database
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Zahtjev ažuriran - Id: {IdZahtjeva}, Naslov zahtjeva: {existingZahtjev.Naslov}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();


                TempData["SuccessMessage"] = "Zahtjev uspješno ažuriran.";

                var callingUrl = HttpContext.Request.Headers["Referer"].ToString();
                string[] routeParams = callingUrl.Split('/');

                if (routeParams[routeParams.Length - 2] == "ZahtjevMD")
                {
                    return RedirectToAction("ZahtjevMD", new { id = IdZahtjeva });
                }
                else
                {
                    return RedirectToAction("Zahtjevi", new { page = page, sort = sort, ascending = ascending });
                }

            }
            catch (Exception ex)
            {
                // Handle exceptions, log the error, or return an appropriate response
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult FormCreateZahtjev(int page, int sort, bool ascending)
        {
            var Projekti = ctx.Projekt.AsNoTracking().ToList();
            var Vrste = ctx.VrstaZah.AsNoTracking().ToList();

            ZahtjevViewModel viewModel = new ZahtjevViewModel(Projekti, Vrste);

            viewModel.PagingInfo = new PagingInfo { CurrentPage = page, Sort = sort, Ascending = ascending };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateZahtjev(string Naslov, string Opis, int? Projekt, int? Vrsta, int page, int sort, bool ascending)
        {
            if (string.IsNullOrWhiteSpace(Naslov))
            {
                TempData["NotFoundMessage"] = "Naslov ne može biti prazan.";
                return RedirectToAction("Zahtjevi");
            }

            try
            {
                Zahtjev zahtjev = new Zahtjev();
                zahtjev.Naslov = Naslov;
                zahtjev.Opis = Opis;
                zahtjev.IdProjekta = Projekt;
                zahtjev.IdVrste = Vrsta;

                ctx.Zahtjev.Add(zahtjev);
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Novi zahtjev dodan - Id: {zahtjev.IdZahtjeva}, Naslov zahtjeva: {zahtjev.Naslov}",
                    Time = DateTime.Now
                });
                ctx.SaveChanges();

                TempData["SuccessMessage"] = "Zahtjev uspješno dodan.";
                return RedirectToAction("Zahtjevi", new { page = page, sort = sort, ascending = ascending });
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ObrisiZahtjev(int id)
        {
            //TO-DO MAKNUT ID FOREING KEY S PROJEKT TABLICE 
            try
            {
                var zahtjev = await ctx.Zahtjev.FindAsync(id);

                if (zahtjev == null)
                {
                    // Return a 404 Not Found status code with a message
                    TempData["NotFoundMessage"] = "Zahtjev ne postoji";
                    return NotFound();
                }

                var zadatci = await ctx.Zadatak.Where(z => z.IdZahtjeva == id).ToListAsync();

                foreach (var zad in zadatci)
                {
                    ctx.LogEntries.Add(new LogEntry
                    {
                        Action = $"Obrisan zadatak - Id: {zad.IdZadatka}, Naslov zadatka: {zad.Naslov}",
                        Time = DateTime.Now
                    });
                    ctx.Zadatak.Remove(zad);
                }

                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Obrisan zahtjev - Id: {zahtjev.IdZahtjeva}, Naslov zahtjeva: {zahtjev.Naslov}",
                    Time = DateTime.Now
                });
                ctx.Zahtjev.Remove(zahtjev);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zahtjev uspješno izbrisan";

                return RedirectToAction("Zahtjevi");
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        [HttpGet]
        public IActionResult VrsteZahtjeva(int page = 1, int sort = 1, bool ascending = true)
        {
            //var vrsteZahtjeva = await ctx.VrstaZah.Include(x => x.Zahtjev).AsNoTracking().ToListAsync();

            //ZahtjevViewModel vrste = new ZahtjevViewModel(vrsteZahtjeva);


            int pagesize = appSettings.PageSize;


            var query = ctx.VrstaZah.Include(x => x.Zahtjev).AsNoTracking();

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
                return RedirectToAction(nameof(VrsteZahtjeva), new { page = 1, sort, ascending });
            }

            query = query.ApplySort(sort, ascending);

            var vrsteZahtjeva = query.Skip((page - 1) * pagesize).Take(pagesize).ToList();

            var model = new ZahtjevViewModel
            {
                VrstaZahtjeva = vrsteZahtjeva,
                PagingInfo = pagingInfo
            };

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> FormUpdateVrstaZahtjev(int id)
        {
            var vrstaZah = await ctx.VrstaZah.FindAsync(id);

            if (vrstaZah == null)
            {
                // Handle the case where no Zahtjev with the given ID is found
                return NotFound();
            }

            ZahtjevViewModel model = new ZahtjevViewModel(vrstaZah);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVrstaZahtjev(int IdVrsteZah, String Ime)
        {

            try
            {
                // Retrieve the existing Zahtjev entity from the database using its ID
                var existingVrstaZahtjev = await ctx.VrstaZah.FindAsync(IdVrsteZah);

                if (existingVrstaZahtjev == null)
                {
                    TempData["NotFoundMessage"] = "Vrsta zahtjeva ne postoji.";
                    return NotFound(); // Handle the case where the Zahtjev is not found
                }

                // Update the properties of the existing Zahtjev entity with new values
                existingVrstaZahtjev.ImeZahtjeva = Ime;

                // Save changes back to the database
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Ažurirana vrsta zahtjeva - Id: {existingVrstaZahtjev.IdVrste}, Ime vrste: {existingVrstaZahtjev.IdVrste}",
                    Time = DateTime.Now
                });
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vrsta zahtjeva uspješno ažurirana.";
                //return RedirectToAction("FormUpdateVrstaZahtjev", new { id = IdVrsteZah });
                return RedirectToAction("VrsteZahtjeva", new { page = 1, sort = 1, ascending = true });

            }
            catch (Exception ex)
            {
                // Handle exceptions, log the error, or return an appropriate response
                return StatusCode(500, $"Internal Server Error: {ex.Message}");


            }
        }

        [HttpPost]
        public IActionResult CreateVrstaZah(string ImeVrste)
        {
            if (string.IsNullOrWhiteSpace(ImeVrste))
            {
                TempData["NotFoundMessage"] = "Ime vrste zahtjeva ne može biti prazno.";
                return RedirectToAction("VrsteZahtjeva");
            }

            try
            {
                VrstaZah vrstaZah = new VrstaZah();
                vrstaZah.ImeZahtjeva = ImeVrste;

                ctx.VrstaZah.Add(vrstaZah);
                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Stvorena nova vrsta zahtjeva - Id: {vrstaZah.IdVrste}, Ime vrste: {vrstaZah.IdVrste}",
                    Time = DateTime.Now
                });
                ctx.SaveChanges();

                TempData["SuccessMessage"] = "Vrsta zahtjeva uspješno dodana.";
                return RedirectToAction("VrsteZahtjeva");
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteVrstuZahtjeva(int id)
        {
            try
            {
                var vrstaZahtjeva = await ctx.VrstaZah.FindAsync(id);

                if (vrstaZahtjeva == null)
                {
                    // Return a 404 Not Found status code with a message
                    TempData["NotFoundMessage"] = "Vrsta zahtjeva ne postoji";
                    return NotFound();
                }

                // remove idVrste to every Zahtjev that has it
                var zahtjevi = await ctx.Zahtjev.Where(z => z.IdVrste == id).ToListAsync();
                if (zahtjevi != null)
                {
                    foreach (var zahtjev in zahtjevi)
                    {
                        zahtjev.IdVrste = null;
                    }
                }

                ctx.LogEntries.Add(new LogEntry
                {
                    Action = $"Obrisana vrsta zahtjeva - Id: {vrstaZahtjeva.IdVrste}, Ime vrste: {vrstaZahtjeva.IdVrste}",
                    Time = DateTime.Now
                });
                ctx.VrstaZah.Remove(vrstaZahtjeva);
                await ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vrsta zahtjeva uspješno izbrisana";
                return RedirectToAction("VrsteZahtjeva");
            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ZahtjevMD(int id)
        {
            try
            {
                var zahtjev = await ctx.Zahtjev
      .Where(z => z.IdZahtjeva == id)
      .Include(z => z.Zadatak)
          .ThenInclude(x => x.IdOsobaNavigation)
          .Include(z => z.Zadatak)
          .ThenInclude(x => x.IdStatusaNavigation)
      .Include(z => z.IdProjektaNavigation)
      .Include(z => z.IdVrsteNavigation)
      .AsNoTracking()
      .SingleOrDefaultAsync();


                if (zahtjev == null)
                {
                    return NotFound();
                }

                var projekti = await ctx.Projekt
                          .AsNoTracking()
                          .ToListAsync();

                var vrsteZahtjeva = await ctx.VrstaZah
                            .AsNoTracking()
                            .ToListAsync();

                ZahtjevViewModel model = new ZahtjevViewModel(zahtjev, projekti, vrsteZahtjeva);

                return View(model);

            }
            catch (Exception e)
            {
                TempData["ServerError"] = $"Server Error: {e.Message}";
                return StatusCode(500);
            }

        }

        [HttpGet]
        public async Task<IActionResult> SljedeciZahtjev(int currentZahtjevId)
        {
            try
            {
                // Find the current Zahtjev in the database
                var currentZahtjev = await ctx.Zahtjev
                    .Where(z => z.IdZahtjeva == currentZahtjevId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (currentZahtjev == null)
                {
                    return NotFound("Current Zahtjev not found.");
                }


                var zahtjevi = await ctx.Zahtjev.OrderBy(z => z.IdZahtjeva).ToListAsync();

                int index = zahtjevi.FindIndex(z => z.IdZahtjeva == currentZahtjevId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int nextZahtjevId = zahtjevi[(index + 1) % zahtjevi.Count()].IdZahtjeva;

                return RedirectToAction("ZahtjevMD", new { id = nextZahtjevId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrethodniZahtjev(int currentZahtjevId)
        {
            try
            {
                // Find the current Zahtjev in the database
                var currentZahtjev = await ctx.Zahtjev
                    .Where(z => z.IdZahtjeva == currentZahtjevId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (currentZahtjev == null)
                {
                    return NotFound("Current Zahtjev not found.");
                }


                var zahtjevi = await ctx.Zahtjev.OrderBy(z => z.IdZahtjeva).ToListAsync();

                int index = zahtjevi.FindIndex(z => z.IdZahtjeva == currentZahtjevId);

                if (index == -1)
                {
                    return NotFound("Unable to determine the position of the current Zahtjev.");
                }

                int newIndex = (index - 1) % zahtjevi.Count();

                newIndex = newIndex < 0 ? zahtjevi.Count() - 1 : newIndex;

                int prevZahtjevId = zahtjevi[newIndex].IdZahtjeva;

                return RedirectToAction("ZahtjevMD", new { id = prevZahtjevId });

            }
            catch (Exception e)
            {
                return StatusCode(500, $"Server Error: {e.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> ImportZahtjevi(IFormFile file)
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

                        List<Zahtjev> ZahtjevList = new List<Zahtjev>();

                        int startRow = worksheet.Dimension.Start.Row + 1;
                        int endRow = worksheet.Dimension.End.Row;

                        for (int row = startRow; row <= endRow; row++)
                        {

                            // Imena su tipa text pa ne mogu supoređivat text i varcahr

                            string projectName = worksheet.Cells[row, 3].Value?.ToString().Trim();
                            Projekt projekt = await ctx.Projekt
                                .Where(p => EF.Functions.Like(p.ImeProjekta, projectName))  // Use EF.Functions.Like for comparison
                                .FirstOrDefaultAsync();

                            string vrstaName = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            VrstaZah vrsta = await ctx.VrstaZah
                                .Where(v => EF.Functions.Like(v.ImeZahtjeva, vrstaName))
                                .FirstOrDefaultAsync();


                            // Create a new Zahtjev entity
                            var zahtjev = new Zahtjev
                            {
                                Naslov = worksheet.Cells[row, 1].Value?.ToString(),
                                Opis = worksheet.Cells[row, 2].Value?.ToString(),
                            };

                            if (projekt != null)
                            {
                                zahtjev.IdProjektaNavigation = projekt;
                                zahtjev.IdProjekta = projekt.IdProjekta;
                            }

                            if (vrsta != null)
                            {
                                zahtjev.IdVrsteNavigation = vrsta;
                                zahtjev.IdVrste = vrsta.IdVrste;
                            }

                            ZahtjevList.Add(zahtjev);
                        }

                        ctx.AddRange(ZahtjevList);
                        await ctx.SaveChangesAsync();

                        var newPackage = new ExcelPackage();
                        var newWorksheet = newPackage.Workbook.Worksheets.Add("Sheet1");

                        newWorksheet.Cells[1, 1].Value = "Naslov";
                        newWorksheet.Cells[1, 2].Value = "Opis";
                        newWorksheet.Cells[1, 3].Value = "Projekt";
                        newWorksheet.Cells[1, 4].Value = "Vrsta";

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