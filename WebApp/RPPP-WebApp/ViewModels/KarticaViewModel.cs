using RPPP_WebApp.Models;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel entiteta Kartica
    /// </summary>
    public class KarticaViewModel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="kartice">Lista kartica</param>
        public KarticaViewModel(IEnumerable<Kartica> kartice)
        {
            this.Kartice = kartice;
        }
        /// <summary>
        /// Konstruktor bez argumenata
        /// </summary>
        public KarticaViewModel() { }
        /// <summary>
        /// Konstruktor s jednom karticom
        /// </summary>
        /// <param name="kartica">Objekt kartice</param>
        public KarticaViewModel(Kartica kartica) { this.Kartica = kartica; }
        /// <summary>
        /// Konstruktor s listom projekata
        /// </summary>
        /// <param name="projekti">List projekata</param>
        public KarticaViewModel(IEnumerable<Projekt> projekti) {
            this.Projekti = projekti;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="kartica">Objekt kartice</param>
        /// <param name="projekti">List projekata</param>
        public KarticaViewModel(Kartica kartica, IEnumerable<Projekt> projekti)
        {
            this.Projekti = projekti;
            this.Kartica = kartica;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="kartica">Objekt kartice</param>
        /// <param name="projekt">Objekt projekta</param>
        /// <param name="transakcije">Lista transakcije</param>
        public KarticaViewModel(Kartica kartica, Projekt projekt, IEnumerable<Transakcija> transakcije)
        {
            this.Projekt = projekt;
            this.Kartica = kartica;
            this.Transakcije = transakcije;
        }
        /// <summary>
        /// Lista kartica.
        /// </summary>
        public IEnumerable<Kartica> Kartice { get; set; }
        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
        /// <summary>
        /// Lista transakcija.
        /// </summary>
        public List<Transakcija> transakcije { get; set; }
        /// <summary>
        /// Objekt kartice.
        /// </summary>
        public Kartica Kartica { get; set; }
        /// <summary>
        /// Lista projekata.
        /// </summary>
        public IEnumerable<Projekt> Projekti { get; set; }
        /// <summary>
        /// Objekt projekta.
        /// </summary>
        public Projekt Projekt { get; set; }
        /// <summary>
        /// Lista transakcija.
        /// </summary>
        public IEnumerable<Transakcija> Transakcije { get; set; }
        /// <summary>
        /// Lista transakcija.
        /// </summary>
        public IEnumerable<KarticeViewModel> kartice { get; set; }
    }
}
