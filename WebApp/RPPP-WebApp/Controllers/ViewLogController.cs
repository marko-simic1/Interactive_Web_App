using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Controllers
{
    public class ViewLogController : Controller
    {
        private readonly ProjektDbContext _context; 

        public ViewLogController(ProjektDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Show(DateTime dan)
        {
            ViewBag.Dan = dan;

            List<LogEntry> logEntriesFromDatabase = await _context.LogEntries
                .Where(entry => entry.Time.Date == dan.Date)
                .ToListAsync();

            List<LogEntryViewModel> logEntryViewModels = logEntriesFromDatabase
                .Select(entry => new LogEntryViewModel
                {
                    Time = entry.Time,
                    Id = entry.Id,
                    Controller = entry.Controller,
                    Level = entry.Level,
                    Message = entry.Message,
                    Url = entry.Url,
                    Action = entry.Action
                })
                .ToList();

            var viewModel = new ViewLogViewModel
            {
                SelectedDate = dan,
                LogEntries = logEntryViewModels
            };

            return View(viewModel);
        }




    }
}
