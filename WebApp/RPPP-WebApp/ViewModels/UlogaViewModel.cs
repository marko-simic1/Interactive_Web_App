using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o ulogama za prikaz na korisničkom sučelju.
    /// </summary>
    public class UlogaViewModel
    {
        /// <summary>
        /// Konstruktor koji inicijalizira ViewModel s kolekcijom uloga.
        /// </summary>
        /// <param name="uloge">Kolekcija uloga.</param>
        public UlogaViewModel(IEnumerable<Uloga> uloge)
        {
            this.Uloge = uloge;
        }

        /// <summary>
        /// Prazan konstruktor.
        /// </summary>
        public UlogaViewModel() { }

        /// <summary>
        /// Kolekcija uloga.
        /// </summary>
        public IEnumerable<Uloga> Uloge { get; set; }

        /// <summary>
        /// Kolekcija ViewModela za uloge.
        /// </summary>
        public IEnumerable<UlogeViewModel> uloge { get; set; }

        /// <summary>
        /// Podaci o strani za prikazivanje uloga.
        /// </summary>
        public PagingInfo PagingInfo { get; set; }

        /// <summary>
        /// Podaci o pojedinačnoj ulozi.
        /// </summary>
        public Uloga Uloga { get; set; }

        /// <summary>
        /// Lista vrsta uloga.
        /// </summary>
        public List<VrstaUloge> VrsteUloga { get; set; }
    }
}
