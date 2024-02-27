using System;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Klasa koja predstavlja informacije o straničenju (paging) za prikaz podataka.
    /// </summary>
    public class PagingInfo
    {
        /// <summary>
        /// Ukupan broj stavki.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Broj stavki po stranici.
        /// </summary>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// Trenutna stranica.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Zastava koja označava rastući redoslijed.
        /// </summary>
        public bool Ascending { get; set; }

        /// <summary>
        /// Ukupan broj stranica.
        /// </summary>
        public int TotalPages
        {
            get
            {
                return (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
            }
        }

        /// <summary>
        /// Vrijednost za sortiranje.
        /// </summary>
        public int Sort { get; set; }
    }
}
