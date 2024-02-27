using RPPP_WebApp.Models;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    class DokumentacijeViewModel
    {
        /// <summary>
        /// Kolekcija dokumentacija.
        /// </summary>
        public IEnumerable<DokumentacijaViewModel> Dokumentacija { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}