using System;
using System.Collections.Generic;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Prikazni model za prikaz logova na sučelju.
    /// </summary>
    public class ViewLogViewModel
    {
        /// <summary>
        /// Odabrani datum.
        /// </summary>
        public DateTime SelectedDate { get; set; }

        /// <summary>
        /// Lista prikaznih modela unosa logova.
        /// </summary>
        public List<LogEntryViewModel> LogEntries { get; set; }  // Prilagođeni LogEntry u LogEntryViewModel
    }

    /// <summary>
    /// Prikazni model za unos pojedinačnog loga.
    /// </summary>
    public class LogEntryViewModel
    {
        /// <summary>
        /// Vrijeme unosa loga.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Identifikator unosa loga.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Naziv kontrolera.
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// Razina loga.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Poruka loga.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// URL povezan s unosom loga.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Naziv akcije.
        /// </summary>
        public string Action { get; set; }
    }
}
