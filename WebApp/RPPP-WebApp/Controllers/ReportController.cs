using RPPP_WebApp.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfRpt.ColumnsItemsTemplates;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using PdfRpt.FluentInterface;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using OfficeOpenXml;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Controllers;
using RPPP_WebApp;
using SkiaSharp;
using RPPP_WebApp.ViewModels;
using NLog.Targets.Wrappers;
using System.Composition;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace MVC.Controllers
{
    public class ReportController : Controller
    {
        private readonly ProjektDbContext ctx;
        private readonly IWebHostEnvironment environment;
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public ReportController(ProjektDbContext ctx, IWebHostEnvironment environment)
        {
            this.ctx = ctx;
            this.environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ProjektPDF()
        {
            string naslov = "Popis projekata";
            var proj = await ctx.Projekt
                                  .Include(d => d.IdVrsteNavigation)
                                  .AsNoTracking()
                                  .OrderBy(d => d.ImeProjekta)
                                  .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(proj));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Projekt.IdProjekta));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdProjekta");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.ImeProjekta);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("ImeProjekta", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.Kratica);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(2);
                    column.HeaderCell("Kratica", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.Sazetak);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(5);
                    column.HeaderCell("Sazetak", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.DatumPoc);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(1);
                    column.HeaderCell("DatumPoc", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.DatumZav);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(6);
                    column.Width(1);
                    column.HeaderCell("DatumZav", horizontalAlignment: HorizontalAlignment.Center);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.BrKartice);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(7);
                    column.Width(2);
                    column.HeaderCell("BrKartice", horizontalAlignment: HorizontalAlignment.Center);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.IdVrsteNavigation.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(8);
                    column.Width(2);
                    column.HeaderCell("ImeVrste", horizontalAlignment: HorizontalAlignment.Center);
                });
            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjektPDF2(int id)
        {
            string naslov = "Projekt i njegove dokumentacije";
            var entitet = await ctx.Projekt
                                          .Where(d => d.IdProjekta == id)
                                          .Include(d => d.IdVrsteNavigation)
                                          .AsNoTracking()
                                          .OrderBy(d => d.ImeProjekta)
                                          .SingleOrDefaultAsync();

            var entitet2 = await ctx.Dokumentacija
                                .Where(d => d.IdProjekta == id)
                                .Include(d => d.IdProjektaNavigation)
                                .Include(d => d.IdVrsteNavigation)
                                .AsNoTracking()
                                .OrderBy(d => d.ImeDok)
                                .ToListAsync();



            PdfReport report = CreateReport(naslov);

            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.HtmlHeader(rptHeader =>
                {
                    rptHeader.PageHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddPageHeader(pageHeader =>
                    {
                        var message = $"MD za {entitet.ImeProjekta}";
                        return string.Format(@"<table style='width: 100%;font-size:9pt;font-family:tahoma;'>
													<tr>
														<td align='center'>{0}</td>
													</tr>
												</table>", message);
                    });

                    rptHeader.GroupHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        SpacingBeforeTable = 10f,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddGroupHeader(groupHeader =>
                    {
                        var idProjekta = entitet?.IdProjekta ?? 0;
                        var imeProjekta = entitet?.ImeProjekta ?? "NEMA DATOTEKA!";
                        var kratica = entitet.Kratica;
                        var sazetak = entitet.Sazetak;
                        var datumPoc = entitet.DatumPoc;
                        var datumkraj = entitet.DatumZav;
                        var vrsta = entitet.IdVrsteNavigation?.ImeVrste != null ? entitet.IdVrsteNavigation.ImeVrste : "Ne pripada nijednoj vrsti";
                        return string.Format(@"<table style='width: 100%; font-size:9pt;font-family:tahoma;'>
															<tr>
																<td style='width:25%;border-bottom-width:0.2; border-bottom-color:red;border-bottom-style:solid'>Id projekta:</td>
																<td style='width:75%'>{0}</td>
															</tr>
															<tr>
																<td style='width:25%'>Ime projekta:</td>
																<td style='width:75%'>{1}</td>
															</tr>
																<td style='width:25%'>Kratica:</td>
																<td style='width:75%'>{2}</td>
															</tr>
																<td style='width:25%'>Sazetak:</td>
																<td style='width:75%'>{3}</td>
															</tr>
                                                            </tr>
																<td style='width:25%'>Datum pocetka:</td>
																<td style='width:75%'>{4}</td>
															</tr>
                                                            </tr>
																<td style='width:25%'>Datum zavrsetka:</td>
																<td style='width:75%'>{5}</td>
															</tr>
                                                            </tr>
																<td style='width:25%'>Ime vrste:</td>
																<td style='width:75%'>{6}</td>
															</tr>
												</table>", idProjekta, imeProjekta, kratica, sazetak, datumPoc, datumkraj, vrsta);
                    });
                });
            });
            #endregion

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(entitet2));

            report.MainTableColumns(columns =>
            {
                #region Stupci po kojima se grupira
                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(s => s.IdProjekta);
                    column.Group(
                        (val1, val2) =>
                        {
                            return (int)val1 == (int)val2;
                        });
                });
                #endregion
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.IdDok);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdDok", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.ImeDok);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Left);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(4);
                    column.HeaderCell("ImeDok", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.IdVrsteNavigation.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(3);
                    column.HeaderCell("ImeVrste", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.IdProjektaNavigation.ImeProjekta);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(4);
                    column.HeaderCell("ImeProjekta", horizontalAlignment: HorizontalAlignment.Center);
                });
            });
            #endregion

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=master.pdf");
                return File(pdf, "application/pdf");
            }
            else
            {
                return NotFound();
            }

        }

        [HttpGet]
        public async Task<IActionResult> VrstaProjektaPDF()
        {
            string naslov = "Popis vrsta projekta";
            var vrstaProj = await ctx.VrstaProjekta
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(vrstaProj));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(VrstaProjekta.IdVrste));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdVrsteProjekta");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<VrstaProjekta>(x => x.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime vrste", horizontalAlignment: HorizontalAlignment.Center);
                });
            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> DokumentacijaPDF()
        {
            string naslov = "Popis dokumentacija";
            var doku = await ctx.Dokumentacija
                                    .Include(d => d.IdProjektaNavigation)
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeDok)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(doku));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Dokumentacija.IdDok));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdDok");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.ImeDok);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("ImeDok", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.IdVrsteNavigation.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(2);
                    column.HeaderCell("ImeVrste", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Dokumentacija>(x => x.IdProjektaNavigation.ImeProjekta);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(2);
                    column.HeaderCell("ImeProjekta", horizontalAlignment: HorizontalAlignment.Center);
                });

            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VrstaDokumentacijePDF()
        {
            string naslov = "Popis vrsta dokumentacije";
            var vrstaDok = await ctx.VrstaDok
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(vrstaDok));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(VrstaUloge.IdVrste));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("Id vrste dokumentacije");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<VrstaUloge>(x => x.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime vrste dokumentacije", horizontalAlignment: HorizontalAlignment.Center);
                });


            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjektExcel()
        {
            var entitet = await ctx.Projekt
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeProjekta)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis projekta";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Projekt");

                worksheet.Cells[1, 1].Value = "Ime projekta";
                worksheet.Cells[1, 2].Value = "Kratica";
                worksheet.Cells[1, 3].Value = "Id projekta";
                worksheet.Cells[1, 4].Value = "Sažetak";
                worksheet.Cells[1, 5].Value = "DatumPoc";
                worksheet.Cells[1, 6].Value = "DatumZav";
                worksheet.Cells[1, 7].Value = "Broj kartice";
                worksheet.Cells[1, 8].Value = "Ime vrste";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeProjekta;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].Kratica;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].IdProjekta;
                    worksheet.Cells[i + 2, 4].Value = entitet[i].Sazetak;
                    worksheet.Cells[i + 2, 5].Value = entitet[i].DatumPoc.ToString();
                    worksheet.Cells[i + 2, 6].Value = entitet[i].DatumZav.ToString();
                    worksheet.Cells[i + 2, 7].Value = entitet[i].BrKartice.ToString();
                    worksheet.Cells[i + 2, 8].Value = entitet[i].IdVrsteNavigation.ImeVrste;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "projekt.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ProjektExcel2(int id)
        {
            var entitet = await ctx.Projekt
                                    .Where(d => d.IdProjekta == id)
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeProjekta)
                                    .SingleOrDefaultAsync();

            var entitet2 = await ctx.Dokumentacija
                                .Where(d => d.IdProjekta == id)
                                .Include(d => d.IdProjektaNavigation)
                                .Include(d => d.IdVrsteNavigation)
                                .AsNoTracking()
                                .OrderBy(d => d.ImeDok)
                                .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis projekta i njegovih dokumentacija";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("ProjektDokMD");

                worksheet.Cells[1, 1].Value = "Ime projekta";
                worksheet.Cells[1, 2].Value = "Kratica";
                worksheet.Cells[1, 3].Value = "Id projekta";
                worksheet.Cells[1, 4].Value = "Sažetak";
                worksheet.Cells[1, 5].Value = "DatumPoc";
                worksheet.Cells[1, 6].Value = "DatumZav";
                worksheet.Cells[1, 7].Value = "Broj kartice";
                worksheet.Cells[1, 8].Value = "Ime vrste";

                worksheet.Cells[2, 1].Value = entitet.ImeProjekta;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[2, 2].Value = entitet.Kratica;
                worksheet.Cells[2, 3].Value = entitet.IdProjekta;
                worksheet.Cells[2, 4].Value = entitet.Sazetak;
                worksheet.Cells[2, 5].Value = entitet.DatumPoc.ToString();
                worksheet.Cells[2, 6].Value = entitet.DatumZav.ToString();
                worksheet.Cells[2, 7].Value = entitet.BrKartice.ToString();
                worksheet.Cells[2, 8].Value = entitet.IdVrsteNavigation.ImeVrste;

                worksheet.Cells[4, 1].Value = "Ime dokumentacije";
                worksheet.Cells[4, 2].Value = "Id dokumentacije";
                worksheet.Cells[4, 3].Value = "Ime projekta";
                worksheet.Cells[4, 4].Value = "Ime vrste dokumenta";

                for (int i = 0; i < entitet2.Count; i++)
                {
                    int row = i + 5;
                    worksheet.Cells[row, 1].Value = entitet2[i].ImeDok;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 2].Value = entitet2[i].IdDok;
                    worksheet.Cells[row, 3].Value = entitet2[i].IdProjektaNavigation?.ImeProjekta != null ? entitet2[i].IdProjektaNavigation.ImeProjekta : "Ne pripada nijednom projektu";
                    worksheet.Cells[row, 4].Value = entitet2[i].IdVrsteNavigation?.ImeVrste != null ? entitet2[i].IdVrsteNavigation?.ImeVrste : "Nema vrstu";
                }

                worksheet.Cells[1, 1, entitet2.Count + 5, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "projektMD.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> VrstaProjektaExcel()
        {
            var entitet = await ctx.VrstaProjekta
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis vrsta projekta";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Vrsta Projekta");

                worksheet.Cells[1, 1].Value = "Ime vrste";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeVrste;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "vrsteProjekta.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DokumentacijaExcel()
        {
            var entitet = await ctx.Dokumentacija
                                    .Include(d => d.IdProjektaNavigation)
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeDok)
                                    .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis dokumentacija";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Dokumentacije");

                worksheet.Cells[1, 1].Value = "Ime dokumentacije";
                worksheet.Cells[1, 2].Value = "Id dokumentacije";
                worksheet.Cells[1, 3].Value = "Ime projekta";
                worksheet.Cells[1, 4].Value = "Ime vrste dokumenta";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeDok;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].IdDok;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].IdProjektaNavigation?.ImeProjekta != null ? entitet[i].IdProjektaNavigation.ImeProjekta : "Ne pripada nijednom projektu";
                    worksheet.Cells[i + 2, 4].Value = entitet[i].IdVrsteNavigation?.ImeVrste != null ? entitet[i].IdVrsteNavigation?.ImeVrste : "Nema vrstu";
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 3].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "dokumentacija.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> VrstaDokumentacijeExcel()
        {
            var entitet = await ctx.VrstaDok
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis vrsta dokumentacija";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Vrste dokumentacija");

                worksheet.Cells[1, 1].Value = "Ime vrste dokumentacije";
                worksheet.Cells[1, 2].Value = "Id vrste dokumentacije";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeVrste;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].IdVrste;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 2].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "vrstedokumentacije.xlsx");
        }


        // Hasnek pdfs and excels START
        [HttpGet]
        public async Task<IActionResult> ZahtjeviPdf()
        {
            string naslov = "Svi zahtjevi";
            var zahtjevi = await ctx.Zahtjev
                                     .Include(z => z.IdProjektaNavigation)
                                     .Include(z => z.IdVrsteNavigation)
                                     .Include(z => z.Zadatak)
                                     .AsNoTracking()
                                     .ToListAsync();

            PdfReport report = CreateReport(naslov);


            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zahtjevi));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Zahtjev.IdZahtjeva));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("Id zahtjeva");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtjev>(x => x.Naslov);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(2);
                    column.HeaderCell("Naslov", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtjev>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

                // nejde :(

                //columns.AddColumn(column =>
                //{
                //    column.PropertyName<Zahtjev>(x => x.IdProjektaNavigation.ImeProjekta != null ? x.IdProjektaNavigation.ImeProjekta : "Ne pripada nijednom projektu");
                //    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                //    column.IsVisible(true);
                //    column.Order(5);
                //    column.Width(3);
                //    column.HeaderCell("Projekt", horizontalAlignment: HorizontalAlignment.Center);
                //});

                //columns.AddColumn(column =>
                //{
                //    column.PropertyName<Zahtjev>(x => x.IdVrsteNavigation.ImeZahtjeva);
                //    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                //    column.IsVisible(true);
                //    column.Order(6);
                //    column.Width(1);
                //    column.HeaderCell("Vrsta Zahtjeva", horizontalAlignment: HorizontalAlignment.Center);
                //});

                //columns.AddColumn(column =>
                //{
                //    column.PropertyName<Zahtjev>(x => x.Zadatak.Count());
                //    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                //    column.IsVisible(true);
                //    column.Order(6);
                //    column.Width(1);
                //    column.HeaderCell("Broj zadataka", horizontalAlignment: HorizontalAlignment.Center);
                //});
            });
            #endregion

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=zahtjevi.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "zahtjevi.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ZahtjeviExcel()
        {
            var zahtjevi = await ctx.Zahtjev
                                     .Include(z => z.IdProjektaNavigation)
                                     .Include(z => z.IdVrsteNavigation)
                                     .Include(z => z.Zadatak)
                                     .AsNoTracking()
                                     .ToListAsync();

            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis zahtjeva";
                excel.Workbook.Properties.Author = "Kristijan Hasnek, rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Zahtjevi");


                worksheet.Cells[1, 1].Value = "Naslov";
                worksheet.Cells[1, 2].Value = "Opis";
                worksheet.Cells[1, 3].Value = "Projekt";
                worksheet.Cells[1, 4].Value = "Vrsta";


                for (int i = 0; i < zahtjevi.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = zahtjevi[i].Naslov;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = zahtjevi[i].Opis;
                    worksheet.Cells[i + 2, 3].Value = zahtjevi[i].IdProjektaNavigation?.ImeProjekta != null ? zahtjevi[i].IdProjektaNavigation.ImeProjekta : "Ne pripada nijednom projektu";
                    worksheet.Cells[i + 2, 4].Value = zahtjevi[i].IdVrsteNavigation?.ImeZahtjeva != null ? zahtjevi[i].IdVrsteNavigation?.ImeZahtjeva : "Nema vrstu";
                }

                worksheet.Cells[1, 1, zahtjevi.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "zahtjevi.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ZadaciPdf()
        {
            string naslov = "Svi zadaci";
            var zadaci = await ctx.Zadatak.AsNoTracking().ToListAsync();

            PdfReport report = CreateReport(naslov);


            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zadaci));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Zadatak.IdZadatka));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Id zadatka");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Naslov);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(3);
                    column.HeaderCell("Naslov", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(4);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.PlanPocetak);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Planirani početka", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.PlanKraj);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Planirani kraja", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Prioritet);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Prioritet", horizontalAlignment: HorizontalAlignment.Center);
                });

            });
            #endregion

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=zadaci.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "zahtjevi.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ZadaciExcel()
        {
            var zadaci = await ctx.Zadatak
                                        .Include(z => z.IdStatusaNavigation)
                                        .Include(z => z.IdZahtjevaNavigation)
                                        .AsNoTracking()
                                        .ToListAsync();

            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis zahtjeva";
                excel.Workbook.Properties.Author = "Kristijan Hasnek, rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Zahtjevi");


                worksheet.Cells[1, 1].Value = "Naslov";
                worksheet.Cells[1, 2].Value = "Opis";
                worksheet.Cells[1, 3].Value = "Planirani Početak";
                worksheet.Cells[1, 4].Value = "Planirani Kraj";
                worksheet.Cells[1, 5].Value = "Stvarni Početak";
                worksheet.Cells[1, 6].Value = "Stvarni Kraj";
                worksheet.Cells[1, 7].Value = "Prioritet";
                worksheet.Cells[1, 8].Value = "Zahtjev";
                worksheet.Cells[1, 9].Value = "Status";

                worksheet.Cells[3, 6, zadaci.Count + 1, 6].Style.Numberformat.Format = "mm/dd/yyyy";

                for (int i = 0; i < zadaci.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = zadaci[i].Naslov;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = zadaci[i].Opis;
                    worksheet.Cells[i + 2, 3].Value = zadaci[i].PlanPocetak.ToString();
                    worksheet.Cells[i + 2, 4].Value = zadaci[i].PlanKraj.ToString();
                    worksheet.Cells[i + 2, 5].Value = zadaci[i].StvPoc != null ? zadaci[i].StvPoc.ToString() : "Nije još započeo";
                    worksheet.Cells[i + 2, 6].Value = zadaci[i].StvKraj != null ? zadaci[i].StvKraj.ToString() : "Nije još završio";
                    worksheet.Cells[i + 2, 7].Value = zadaci[i].Prioritet;
                    worksheet.Cells[i + 2, 8].Value = zadaci[i].IdZahtjevaNavigation?.Naslov != null ? zadaci[i].IdZahtjevaNavigation?.Naslov : "Nema zahtjev";
                    worksheet.Cells[i + 2, 9].Value = zadaci[i].IdStatusaNavigation?.ImeStatusa != null ? zadaci[i].IdStatusaNavigation?.ImeStatusa : "Nema status";
                }

                worksheet.Cells[1, 1, zadaci.Count + 1, 9].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "zadaci.xlsx");
        }

        public async Task<IActionResult> ZahtjevMdPdf(int id)
        {
            var zahtjev = await ctx.Zahtjev
                                        .Where(z => z.IdZahtjeva == id)
                                        .Include(z => z.IdProjektaNavigation)
                                        .AsNoTracking()
                                        .SingleOrDefaultAsync();

            var zadaci = await ctx.Zadatak
                                        .Where(z => z.IdZahtjeva == id)
                                        .AsNoTracking()
                                        .ToListAsync();

            PdfReport report = CreateReport(zahtjev.Naslov);

            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.HtmlHeader(rptHeader =>
                {
                    rptHeader.PageHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddPageHeader(pageHeader =>
                    {
                        var message = $"MD za {zahtjev.Naslov}";
                        return string.Format(@"<table style='width: 100%;font-size:9pt;font-family:tahoma;'>
													<tr>
														<td align='center'>{0}</td>
													</tr>
												</table>", message);
                    });

                    rptHeader.GroupHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        SpacingBeforeTable = 10f,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddGroupHeader(groupHeader =>
                    {
                        var idZahtjeva = zahtjev.IdZahtjeva;
                        var naslov = zahtjev.Naslov;
                        var opis = zahtjev.Opis;
                        var projekt = zahtjev.IdProjektaNavigation?.ImeProjekta != null ? zahtjev.IdProjektaNavigation.ImeProjekta : "Ne pripada nijednom projektu";
                        return string.Format(@"<table style='width: 100%; font-size:9pt;font-family:tahoma;'>
															<tr>
																<td style='width:25%;border-bottom-width:0.2; border-bottom-color:red;border-bottom-style:solid'>Id zahtjeva:</td>
																<td style='width:75%'>{0}</td>
															</tr>
															<tr>
																<td style='width:25%'>Naslov:</td>
																<td style='width:75%'>{1}</td>
															</tr>
																<td style='width:25%'>Opis:</td>
																<td style='width:75%'>{2}</td>
															</tr>
																<td style='width:25%'>Projekt:</td>
																<td style='width:75%'>{3}</td>
															</tr>
												</table>", idZahtjeva, naslov, opis, projekt);
                    });
                });
            });
            #endregion

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zadaci));

            report.MainTableColumns(columns =>
            {
                #region Stupci po kojima se grupira
                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtjev>(s => s.IdZahtjeva);
                    column.Group(
                        (val1, val2) =>
                        {
                            return (int)val1 == (int)val2;
                        });
                });
                #endregion
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Naslov);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Width(3);
                    column.HeaderCell("Naslov", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Left);
                    column.IsVisible(true);
                    column.Width(5);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.PlanPocetak);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(2);
                    column.HeaderCell("Planirani početak", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.PlanKraj);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(2);
                    column.HeaderCell("Plan kraj", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Prioritet);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(2);
                    column.HeaderCell("Prioritet", horizontalAlignment: HorizontalAlignment.Center);
                });
            });
            #endregion

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=master.pdf");
                return File(pdf, "application/pdf");
            }
            else
            {
                return NotFound();
            }

        }

        public async Task<IActionResult> ZahtjevMdExcel(int id)
        {
            var zahtjev = await ctx.Zahtjev
                                        .Where(z => z.IdZahtjeva == id)
                                        .Include(z => z.IdProjektaNavigation)
                                        .AsNoTracking()
                                        .SingleOrDefaultAsync();

            var zadaci = await ctx.Zadatak
                                        .Where(z => z.IdZahtjeva == id)
                                        .AsNoTracking()
                                        .ToListAsync();

            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Zahtjev-zadaci-MD";
                excel.Workbook.Properties.Author = "Kristijan Hasnek, rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Zahtjevi-Zadaci MD");


                // Master data
                worksheet.Cells[1, 1].Value = "Zahtjev ID";
                worksheet.Cells[1, 2].Value = "Naslov";
                worksheet.Cells[1, 3].Value = "Opis";

                worksheet.Cells[2, 1].Value = zahtjev.IdZahtjeva;
                worksheet.Cells[2, 2].Value = zahtjev.Naslov;
                worksheet.Cells[2, 3].Value = zahtjev.Opis;

                // Detail header
                worksheet.Cells[4, 1].Value = "Naslov";
                worksheet.Cells[4, 2].Value = "Opis";
                worksheet.Cells[4, 3].Value = "Planirani Početak";
                worksheet.Cells[4, 4].Value = "Planirani Kraj";
                worksheet.Cells[4, 5].Value = "Stvarni Početak";
                worksheet.Cells[4, 6].Value = "Stvarni Kraj";
                worksheet.Cells[4, 7].Value = "Prioritet";
                worksheet.Cells[4, 8].Value = "Status";

                // Detail data
                for (int i = 0; i < zadaci.Count; i++)
                {
                    int row = i + 5;

                    worksheet.Cells[row, 1].Value = zadaci[i].Naslov;
                    worksheet.Cells[row, 2].Value = zadaci[i].Opis;
                    worksheet.Cells[row, 3].Value = zadaci[i].PlanPocetak;
                    worksheet.Cells[row, 4].Value = zadaci[i].PlanKraj;
                    worksheet.Cells[row, 5].Value = zadaci[i].StvPoc != null ? zadaci[i].StvPoc : "Nije još započeo";
                    worksheet.Cells[row, 6].Value = zadaci[i].StvKraj != null ? zadaci[i].StvKraj : "Nije još završio";
                    worksheet.Cells[row, 7].Value = zadaci[i].Prioritet;
                    worksheet.Cells[row, 8].Value = zadaci[i].IdStatusaNavigation?.ImeStatusa != null ? zadaci[i].IdStatusaNavigation?.ImeStatusa : "Nema status";
                }

                worksheet.Cells[1, 1, zadaci.Count + 5, 9].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "MDexcel.xlsx");
        }
        // Hasnek pdfs and excels START

        [HttpGet]
        public async Task<IActionResult> OsobePDF()
        {
            string naslov = "Popis osoba";
            var osobe = await ctx.Osoba
                                  .AsNoTracking()
                                  .OrderBy(d => d.Ime)
                                  .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
    {
        defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
        defaultHeader.Message(naslov);
    });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(osobe));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
          {
              column.IsRowNumber(true);
              column.CellsHorizontalAlignment(HorizontalAlignment.Right);
              column.IsVisible(true);
              column.Order(0);
              column.Width(1);
              column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
          });

                columns.AddColumn(column =>
          {
              column.PropertyName(nameof(Osoba.IdOsoba));
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(1);
              column.Width(1);
              column.HeaderCell("Id Osobe");
          });

                columns.AddColumn(column =>
          {
              column.PropertyName<Osoba>(x => x.Ime);
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(2);
              column.Width(2);
              column.HeaderCell("Ime", horizontalAlignment: HorizontalAlignment.Center);
          });

                columns.AddColumn(column =>
          {
              column.PropertyName<Osoba>(x => x.Prezime);
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(3);
              column.Width(2);
              column.HeaderCell("Prezime", horizontalAlignment: HorizontalAlignment.Center);
          });

                columns.AddColumn(column =>
          {
              column.PropertyName<Osoba>(x => x.Telefon);
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(2);
              column.Width(1);
              column.HeaderCell("Telefon", horizontalAlignment: HorizontalAlignment.Center);
          });

                columns.AddColumn(column =>
          {
              column.PropertyName<Osoba>(x => x.Email);
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(5);
              column.Width(3);
              column.HeaderCell("Email", horizontalAlignment: HorizontalAlignment.Center);
          });

                columns.AddColumn(column =>
          {
              column.PropertyName<Osoba>(x => x.Iban);
              column.CellsHorizontalAlignment(HorizontalAlignment.Center);
              column.IsVisible(true);
              column.Order(6);
              column.Width(3);
              column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
          });
            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> OsobePDF2(int id)
        {
            string naslov = "Popis osoba i uloga";
            var osobe = await ctx.Osoba
                          .Where(z => z.IdOsoba == id)
                          .Include(z => z.RadiNa)
                          .ThenInclude(x => x.IdUlogeNavigation)
                          .AsNoTracking()
                          .SingleOrDefaultAsync();



            PdfReport report = CreateReport(naslov);

            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true);
                header.DefaultHeader(defaultHeader =>
    {
        defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
        defaultHeader.Message(naslov);
    });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Ime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Ime", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Prezime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Prezime", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Telefon);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(1);
                    column.HeaderCell("Telefon", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Email);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(3);
                    column.HeaderCell("Email", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Iban);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
                });
            });


            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=osobe.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "osobe.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PartneriPDF()
        {
            string naslov = "Popis partnera";
            var partneri = await ctx.Partner
                                    .AsNoTracking()
                                    .OrderBy(d => d.Ime)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(partneri));


            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {

                    column.PropertyName<Osoba>(x => x.Ime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Ime", horizontalAlignment: HorizontalAlignment.Center);

                });

                columns.AddColumn(column =>
                {

                    column.PropertyName<Osoba>(x => x.Prezime);

                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Prezime", horizontalAlignment: HorizontalAlignment.Center);

                });

                columns.AddColumn(column =>
                {

                    column.PropertyName<Osoba>(x => x.Telefon);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(1);
                    column.HeaderCell("Telefon", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {

                    column.PropertyName<Osoba>(x => x.Email);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(3);
                    column.HeaderCell("Email", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Iban);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
                });
            });


            #endregion
            byte[] pdf = report.GenerateAsByteArray();

      if (pdf != null)
      {
        Response.Headers.Add("content-disposition", "inline; filename=osobe.pdf");
        //return File(pdf, "application/pdf");
        return File(pdf, "application/pdf", "osobe.pdf"); 
      }
      else
      {
        return NotFound();
      }
    }

        [HttpGet]
        public async Task<IActionResult> UlogePDF()
        {
            string naslov = "Popis uloga";
            var uloge = await ctx.Uloga
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeUloge)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(uloge));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Uloga.IdUloge));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdUloge");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Uloga>(x => x.ImeUloge);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Uloga>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(8);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VrsteUlogaPDF()
        {
            string naslov = "Popis vrsta uloga";
            var uloge = await ctx.VrstaUloge
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(uloge));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(VrstaUloge.IdVrste));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("Id vrste uloge");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<VrstaUloge>(x => x.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime vrste uloge", horizontalAlignment: HorizontalAlignment.Center);
                });


            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> OsobaExcel()
        {
            var entitet = await ctx.Osoba
                                    .AsNoTracking()
                                    .OrderBy(d => d.Ime)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis osoba";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Osobe");

                worksheet.Cells[1, 1].Value = "Ime";
                worksheet.Cells[1, 2].Value = "Prezime";
                worksheet.Cells[1, 3].Value = "Id Osobe";
                worksheet.Cells[1, 4].Value = "Telefon";
                worksheet.Cells[1, 5].Value = "Email";
                worksheet.Cells[1, 6].Value = "Iban";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].Ime;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].Prezime;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].IdOsoba;
                    worksheet.Cells[i + 2, 4].Value = entitet[i].Telefon;
                    worksheet.Cells[i + 2, 5].Value = entitet[i].Email;
                    worksheet.Cells[i + 2, 6].Value = entitet[i].Iban;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "osobe.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> PartnerExcel()
        {
            var entitet = await ctx.Partner
                                    .AsNoTracking()
                                    .OrderBy(d => d.Ime)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis partnera";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Partneri");

                worksheet.Cells[1, 1].Value = "Ime";
                worksheet.Cells[1, 2].Value = "Oib";
                worksheet.Cells[1, 3].Value = "Telefon";
                worksheet.Cells[1, 4].Value = "Email";
                worksheet.Cells[1, 5].Value = "Iban";
                worksheet.Cells[1, 6].Value = "Adresa";

                    for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].Ime;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].Oib;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].Telefon;
                    worksheet.Cells[i + 2, 4].Value = entitet[i].Email;
                    worksheet.Cells[i + 2, 5].Value = entitet[i].Iban;
                    worksheet.Cells[i + 2, 6].Value = entitet[i].Adresa;
                    }

                worksheet.Cells[1, 1, entitet.Count + 1, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "partneri.xlsx");
        }
    
        [HttpGet]
        public async Task<IActionResult> UlogaExcel()
        {
            var entitet = await ctx.Uloga
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeUloge)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis uloga";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Uloge");

                worksheet.Cells[1, 1].Value = "Ime uloge";
                worksheet.Cells[1, 2].Value = "Id Uloge";
                worksheet.Cells[1, 3].Value = "Opis";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeUloge;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].IdUloge;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].Opis;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 3].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "uloge.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> VrsteUlogaExcel()
        {
            var entitet = await ctx.VrstaUloge
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis vrsta uloga";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Vrste uloga");

                worksheet.Cells[1, 1].Value = "Ime vrste uloge";
                worksheet.Cells[1, 2].Value = "Id vrste uloge";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeVrste;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].IdVrste;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 2].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "vrsteuloga.xlsx");
        }
    
        
    
        private PdfReport CreateReport(string naslov)
    {
      var pdf = new PdfReport();

      pdf.DocumentPreferences(doc =>
      {
        doc.Orientation(PageOrientation.Portrait);
        doc.PageSize(PdfPageSize.A4);
        doc.DocumentMetadata(new DocumentMetadata
        {
          Author = "rppp02",
          Application = "RPPP_WebApp Core",
          Title = naslov
        });
        doc.Compression(new CompressionSettings
        {
          EnableCompression = true,
          EnableFullCompression = true
        });
      })
      //fix za linux https://github.com/VahidN/PdfReport.Core/issues/40
      .DefaultFonts(fonts => {
        fonts.Path(Path.Combine(environment.WebRootPath, "fonts", "verdana.ttf"),
                         Path.Combine(environment.WebRootPath, "fonts", "tahoma.ttf"));
        fonts.Size(9);
        fonts.Color(System.Drawing.Color.Black);
      })
      //
      .MainTableTemplate(template =>
      {
        template.BasicTemplate(BasicTemplate.ProfessionalTemplate);
      })
      .MainTablePreferences(table =>
      {
        table.ColumnsWidthsType(TableColumnWidthType.Relative);
              //table.NumberOfDataRowsPerPage(20);
              table.GroupsPreferences(new GroupsPreferences
        {
          GroupType = GroupType.HideGroupingColumns,
          RepeatHeaderRowPerGroup = true,
          ShowOneGroupPerPage = true,
          SpacingBeforeAllGroupsSummary = 5f,
          NewGroupAvailableSpacingThreshold = 150,
          SpacingAfterAllGroupsSummary = 5f
        });
        table.SpacingAfter(4f);
      });

      return pdf;
    }
        [HttpGet]
        public async Task<IActionResult> PosloviPDF()
        {
            string naslov = "Popis poslova";
            var osobe = await ctx.Posao
                            .AsNoTracking()
                            .OrderBy(d => d.ImePosla)
                            .ToListAsync();
            PdfReport report = CreateReport(naslov);
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(osobe));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Posao.IdPosla));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdPosla");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Posao>(x => x.ImePosla);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("ImePOsla", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Posao>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(2);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Posao>(x => x.IdVrsteNavigation.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(5);
                    column.HeaderCell("Ime vrste", horizontalAlignment: HorizontalAlignment.Center);
                });



            });
            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> PosloviPDF2(int id)
        {
            string naslov = "Popis poslova i uloga";
            var poslovi = await ctx.Posao
                          .Where(z => z.IdPosla == id)
                          .Include(z => z.IdOsoba)


                          .AsNoTracking()
                          .SingleOrDefaultAsync();



            PdfReport report = CreateReport(naslov);

            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true);
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Posao>(x => x.ImePosla);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Ime posla", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Posao>(x => x.Opis);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Opis", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Ime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(1);
                    column.HeaderCell("Telefon", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Prezime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(4);
                    column.Width(3);
                    column.HeaderCell("Email", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Telefon);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Iban);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Osoba>(x => x.Email);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Iban", horizontalAlignment: HorizontalAlignment.Center);
                });
            });


            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=osobe.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "osobe.pdf");
            }
            else
            {
                return NotFound();
            }
        }


        [HttpGet]
        public async Task<IActionResult> PosaoExcel()
        {
            var entitet = await ctx.Posao
                                    //.Where(d => d.IdPosla == id)
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImePosla)
                                    .ToListAsync();


            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis poslova";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Poslovi");

                worksheet.Cells[1, 1].Value = "Ime posla";
                worksheet.Cells[1, 2].Value = "Opis posla";
                worksheet.Cells[1, 3].Value = "Opis posla";



                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImePosla;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].Opis;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].IdVrsteNavigation.ImeVrste;


                }

                worksheet.Cells[1, 1, entitet.Count + 1, 3].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "poslovi.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> VrstePoslovaPDF()
        {
            string naslov = "Popis vrsta uloga";
            var uloge = await ctx.VrstaPosla
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(uloge));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(VrstaPosla.IdVrste));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("Id vrste posla");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<VrstaPosla>(x => x.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime vrste posla", horizontalAlignment: HorizontalAlignment.Center);
                });


            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VrstePoslovaExcel()
        {
            var entitet = await ctx.VrstaPosla
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImeVrste)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis vrsta poslova";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Vrste poslova");

                worksheet.Cells[1, 1].Value = "Ime vrste poslova";
                worksheet.Cells[1, 2].Value = "Id vrste poslova";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].ImeVrste;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].IdVrste;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 2].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "vrsteuloga.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ProjektiiPDF()
        {
            string naslov = "Popis projekata";
            var partneri = await ctx.Projekt
                                    .AsNoTracking()
                                    .OrderBy(d => d.IdProjekta)
                                    .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(partneri));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Projekt.IdProjekta));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("IdProjekta");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.ImeProjekta);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Ime projekta", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.Kratica);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(2);
                    column.HeaderCell("Kratica", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.Sazetak);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(1);
                    column.HeaderCell("Sažetak", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.DatumPoc);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(3);
                    column.HeaderCell("Datum početka", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.DatumZav);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(6);
                    column.Width(3);
                    column.HeaderCell("Datum završetka", horizontalAlignment: HorizontalAlignment.Center);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Projekt>(x => x.BrKartice);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(7);
                    column.Width(3);
                    column.HeaderCell("Broj kartice", horizontalAlignment: HorizontalAlignment.Center);
                });
            });



            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=drzave.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "drzave.pdf");
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProjekttExcel()
        {
            var entitet = await ctx.Projekt
                                    .AsNoTracking()
                                    .OrderBy(d => d.IdProjekta)
                                    .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis projekata";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Projekti");

                worksheet.Cells[1, 1].Value = "Id projekta";
                worksheet.Cells[1, 2].Value = "Ime projekta";
                worksheet.Cells[1, 3].Value = "Kratica";
                worksheet.Cells[1, 4].Value = "Sazetak";
                worksheet.Cells[1, 5].Value = "Datum početka";
                worksheet.Cells[1, 6].Value = "Datum kraja";
                worksheet.Cells[1, 7].Value = "Broj kartice";

                for (int i = 0; i < entitet.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = entitet[i].IdProjekta;
                    worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 2].Value = entitet[i].ImeProjekta;
                    worksheet.Cells[i + 2, 3].Value = entitet[i].Kratica;
                    worksheet.Cells[i + 2, 4].Value = entitet[i].Sazetak;
                    worksheet.Cells[i + 2, 5].Value = entitet[i].DatumPoc;
                    worksheet.Cells[i + 2, 6].Value = entitet[i].DatumZav;
                    worksheet.Cells[i + 2, 7].Value = entitet[i].BrKartice;
                }

                worksheet.Cells[1, 1, entitet.Count + 1, 7].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "projekti.xlsx");
        }
        [HttpGet]
        public async Task<IActionResult> PosaoExcel2(int id)
        {
            var entitet = await ctx.Posao
                                    .Where(d => d.IdPosla == id)
                                    .Include(d => d.IdVrsteNavigation)
                                    .AsNoTracking()
                                    .OrderBy(d => d.ImePosla)
                                    .SingleOrDefaultAsync();

            var entitet2 = await ctx.Osoba
                            .Where(o => o.IdPosla.Any(osoba => osoba.IdPosla == id))
                            .AsNoTracking()
                            .OrderBy(o => o.IdOsoba)  // Assuming IdOsoba is the property you want to order by
                            .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis poslova i osoba";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("ProjektDokMD");

                worksheet.Cells[1, 1].Value = "Ime posla";
                worksheet.Cells[1, 2].Value = "Opis";
                worksheet.Cells[1, 3].Value = "Ime vrste";


                worksheet.Cells[2, 1].Value = entitet.ImePosla;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[2, 2].Value = entitet.Opis;
                worksheet.Cells[2, 3].Value = entitet.IdVrsteNavigation.ImeVrste;

                worksheet.Cells[4, 1].Value = "Ime";
                worksheet.Cells[4, 2].Value = "Prezime";
                worksheet.Cells[4, 3].Value = "Email";
                worksheet.Cells[4, 4].Value = "Telefon";
                worksheet.Cells[4, 5].Value = "Iban";

                for (int i = 0; i < entitet2.Count; i++)
                {
                    int row = i + 5;
                    worksheet.Cells[row, 1].Value = entitet2[i].Ime != null ? entitet2[i].Ime : "Nema vrstu";
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 2].Value = entitet2[i].Prezime != null ? entitet2[i].Prezime : "Nema vrstu";
                    worksheet.Cells[row, 3].Value = entitet2[i].Email != null ? entitet2[i].Email : "Nema vrstu";
                    worksheet.Cells[row, 4].Value = entitet2[i].Telefon != null ? entitet2[i].Telefon : "Nema vrstu";
                    worksheet.Cells[row, 5].Value = entitet2[i].Iban != null ? entitet2[i].Iban : "Nema vrstu";

                }

                worksheet.Cells[1, 1, entitet2.Count + 5, 6].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "projektMD.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> KarticePDF()
        {
            string naslov = "Popis kartica";
            var kartice = await ctx.Kartica
                                  .AsNoTracking()
                                  .OrderBy(k => k.BrKartice)
                                  .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(kartice));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Kartica.BrKartice));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("Br. kartice");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Kartica>(k => k.Stanje);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Stanje", horizontalAlignment: HorizontalAlignment.Center);
                });

            });

            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=kartice.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "kartice.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> TransakcijePDF()
        {
            string naslov = "Popis transakcija";
            var transakcije = await ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
                .AsNoTracking()
                .OrderBy(t => t.IdTrans)
                .ToListAsync();

            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(transakcije));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Transakcija.IdTrans));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("ID");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Model);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Model", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.PozivNaBr);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Poziv na br.", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.BrKartice);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Br. kartice", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Datum);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Vrijeme", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Iznos);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Iznos", horizontalAlignment: HorizontalAlignment.Center);
                });

            });

            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=transakcije.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "transakcije.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VrsteTransakcijaPDF()
        {
            string naslov = "Popis vrsta transakcija";
            var vrste = await ctx.VrstaTransakcije
                                  .AsNoTracking()
                                  .OrderBy(v => v.IdVrste)
                                  .ToListAsync();
            PdfReport report = CreateReport(naslov);
            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });
            #endregion
            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(vrste));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(VrstaTransakcije.IdVrste));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("ID");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<VrstaTransakcije>(v => v.ImeVrste);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Vrsta", horizontalAlignment: HorizontalAlignment.Center);
                });

            });

            #endregion
            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=vrste_transakcija.pdf");
                //return File(pdf, "application/pdf");
                return File(pdf, "application/pdf", "vrste_transakcija.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> KarticeExcel()
        {
            var kartice = await ctx.Kartica
                .Include(k => k.Projekt)
                .AsNoTracking()
                .OrderBy(k => k.BrKartice)
                .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis kartica";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Kartice");

                worksheet.Cells[1, 1].Value = "Br. kartice";
                worksheet.Cells[1, 2].Value = "Stanje";
                worksheet.Cells[1, 3].Value = "Projekt";

                for (int i = 0; i < kartice.Count; i++)
                {
                    var projekt = kartice[i].Projekt != null ? kartice[i].Projekt.FirstOrDefault() : null;
                    worksheet.Cells[i + 2, 1].Value = kartice[i].BrKartice;
                    worksheet.Cells[i + 2, 2].Value = kartice[i].Stanje;
                    worksheet.Cells[i + 2, 3].Value = projekt != null ? projekt.ImeProjekta : "-";
                }

                worksheet.Cells[1, 1, kartice.Count + 1, 3].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "kartice.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> TransakcijeExcel()
        {
            var transakcije = await ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
                .AsNoTracking()
                .OrderBy(t => t.IdTrans)
                .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis transakcija";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Transakcije");

                worksheet.Cells[1, 1].Value = "Model";
                worksheet.Cells[1, 2].Value = "Poziv na br.";
                worksheet.Cells[1, 3].Value = "Vrsta";
                worksheet.Cells[1, 4].Value = "Opis";
                worksheet.Cells[1, 5].Value = "Br. kartice";
                worksheet.Cells[1, 6].Value = "Vrijeme";
                worksheet.Cells[1, 7].Value = "Iznos";

                for (int i = 0; i < transakcije.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = transakcije[i].Model;
                    worksheet.Cells[i + 2, 2].Value = transakcije[i].PozivNaBr;
                    worksheet.Cells[i + 2, 3].Value = transakcije[i].IdVrsteNavigation != null ? transakcije[i].IdVrsteNavigation.ImeVrste : "-";
                    worksheet.Cells[i + 2, 4].Value = transakcije[i].Opis;
                    worksheet.Cells[i + 2, 5].Value = transakcije[i].BrKartice;
                    worksheet.Cells[i + 2, 6].Value = transakcije[i].Datum;
                    worksheet.Cells[i + 2, 7].Value = transakcije[i].Iznos;
                }

                worksheet.Cells[1, 1, transakcije.Count + 1, 7].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "transakcije.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> VrsteTransakcijaExcel()
        {
            var vrste = await ctx.VrstaTransakcije
                .AsNoTracking()
                .OrderBy(v => v.IdVrste)
                .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis vrsta transakcija";
                excel.Workbook.Properties.Author = "rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Vrste transakcija");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Vrsta";

                for (int i = 0; i < vrste.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = vrste[i].IdVrste;
                    worksheet.Cells[i + 2, 2].Value = vrste[i].ImeVrste;
                }

                worksheet.Cells[1, 1, vrste.Count + 1, 2].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "vrste_transakcija.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> KarticaMDExcel(int brKartice)
        {
            var kartica = await ctx.Kartica
                .Where(k => k.BrKartice == brKartice)
                .Include(k => k.Projekt)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var transakcije = await ctx.Transakcija
                .Include(t => t.IdVrsteNavigation)
                .Where(t => t.BrKartice == brKartice)
                .AsNoTracking()
                .ToListAsync();

            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Kartica - Transakcije";
                excel.Workbook.Properties.Author = "Fran Zlatar, rppp-02";
                var worksheet = excel.Workbook.Worksheets.Add("Kartica - Transakcije");


                // Master data
                worksheet.Cells[1, 1].Value = "Br. kartice";
                worksheet.Cells[1, 2].Value = "Stanje";
                worksheet.Cells[1, 3].Value = "Projekt";

                worksheet.Cells[2, 1].Value = kartica.BrKartice;
                worksheet.Cells[2, 2].Value = kartica.Stanje;
                worksheet.Cells[2, 3].Value = kartica.Projekt != null ? kartica.Projekt.FirstOrDefault().ImeProjekta : "Nema projekta";

                worksheet.Cells[4, 1].Value = "Model";
                worksheet.Cells[4, 2].Value = "Poziv na br.";
                worksheet.Cells[4, 3].Value = "Vrsta";
                worksheet.Cells[4, 4].Value = "Opis";
                worksheet.Cells[4, 5].Value = "Br. kartice";
                worksheet.Cells[4, 6].Value = "Vrijeme";
                worksheet.Cells[4, 7].Value = "Iznos";

                for (int i = 0; i < transakcije.Count; i++)
                {
                    int row = i + 5;

                    worksheet.Cells[row, 1].Value = transakcije[i].Model;
                    worksheet.Cells[row, 2].Value = transakcije[i].PozivNaBr;
                    worksheet.Cells[row, 3].Value = transakcije[i].IdVrsteNavigation != null ? transakcije[i].IdVrsteNavigation.ImeVrste : "-";
                    worksheet.Cells[row, 4].Value = transakcije[i].Opis;
                    worksheet.Cells[row, 5].Value = transakcije[i].BrKartice;
                    worksheet.Cells[row, 6].Value = transakcije[i].Datum;
                    worksheet.Cells[row, 7].Value = transakcije[i].Iznos;
                }

                worksheet.Cells[1, 1, transakcije.Count + 5, 7].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "Kartica-Transakcije.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> KarticaMDPdf(int brKartice)
        {
            var kartica = await ctx.Kartica
                .Where(k => k.BrKartice == brKartice)
                .Include(k => k.Projekt)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var transakcije = await ctx.Transakcija
                .Where(t => t.BrKartice == brKartice)
                .AsNoTracking()
                .ToListAsync();

            PdfReport report = CreateReport("Kartica - Transakcije popis");

            #region Podnožje i zaglavlje
            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.HtmlHeader(rptHeader =>
                {
                    rptHeader.PageHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddPageHeader(pageHeader =>
                    {
                        var message = $"MD za popis transakcija kartice";
                        return string.Format(@"<table style='width: 100%;font-size:9pt;font-family:tahoma;'>
													<tr>
														<td align='center'>{0}</td>
													</tr>
												</table>", message);
                    });

                    rptHeader.GroupHeaderProperties(new HeaderBasicProperties
                    {
                        RunDirection = PdfRunDirection.LeftToRight,
                        ShowBorder = true,
                        SpacingBeforeTable = 10f,
                        PdfFont = header.PdfFont
                    });
                    rptHeader.AddGroupHeader(groupHeader =>
                    {
                        var brKartice = kartica.BrKartice;
                        var stanje = kartica.Stanje;
                        var projekt = kartica.Projekt != null ? kartica.Projekt.FirstOrDefault().ImeProjekta : "Nema projekta";
                        return string.Format(@"<table style='width: 100%; font-size:9pt;font-family:tahoma;'>
															<tr>
																<td style='width:25%'>Br. kartice:</td>
																<td style='width:75%'>{0}</td>
															</tr>
															<tr>
																<td style='width:25%'>Stanje:</td>
																<td style='width:75%'>{1}</td>
															</tr>
                                                            <tr>
																<td style='width:25%'>Projekt:</td>
																<td style='width:75%'>{2}</td>
															</tr>
												</table>", brKartice, stanje, projekt);
                    });
                });
            });
            #endregion

            #region Postavljanje izvora podataka i stupaca
            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(transakcije));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(Transakcija.IdTrans));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(1);
                    column.HeaderCell("ID");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Model);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Model", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.PozivNaBr);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Poziv na br.", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.BrKartice);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Br. kartice", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Datum);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Vrijeme", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Transakcija>(t => t.Iznos);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Iznos", horizontalAlignment: HorizontalAlignment.Center);
                });
            });
            #endregion

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=Kartica-Transakcije.pdf");
                return File(pdf, "application/pdf");
            }
            else
            {
                return NotFound();
            }

        }

    }
}
