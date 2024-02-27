using RPPP_WebApp.Models;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o osobama za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class OsobaViewModel
    {
        /// <summary>
        /// Konstruktor koji prima kolekciju osoba.
        /// </summary>
        /// <param name="osobe">Kolekcija osoba.</param>
        public OsobaViewModel(IEnumerable<Osoba> osobe)
        {
            this.Osobe = osobe;
        }

        /// <summary>
        /// Prazan konstruktor za inicijalizaciju.
        /// </summary>
        public OsobaViewModel() { }

        /// <summary>
        /// Kolekcija ViewModela za prikaz podataka o osobama.
        /// </summary>
        public IEnumerable<OsobeViewModels> osobe { get; set; }

        /// <summary>
        /// Konstruktor koji prima pojedinu osobu.
        /// </summary>
        /// <param name="osoba">Objekt tipa Osoba.</param>
        public OsobaViewModel(Osoba osoba) { this.Osoba = osoba; }

        /// <summary>
        /// Kolekcija osoba.
        /// </summary>
        public IEnumerable<Osoba> Osobe { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }

        /// <summary>
        /// Lista uloga.
        /// </summary>
        public List<Uloga> uloga { get; set; }

        /// <summary>
        /// Objekt koji predstavlja osobu.
        /// </summary>
        public Osoba Osoba { get; set; }

        /// <summary>
        /// Lista partnera.
        /// </summary>
        public List<Partner> partner { get; set; }

        /// <summary>
        /// Identifikator uloge.
        /// </summary>
        public int UlogaId { get; set; }
    }
}
