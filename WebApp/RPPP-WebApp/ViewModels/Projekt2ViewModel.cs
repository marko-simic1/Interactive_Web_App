using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    public class Projekt2ViewModel
    {

        /// <summary>
        /// Konstruktor koji prima kolekciju projekata.
        /// </summary>
        /// <param name="projekti">Kolekcija projekata.</param>
        public Projekt2ViewModel(IEnumerable<Projekt> projekti)
        {
            this.Projekti = projekti;
        }

        /// <summary>
        /// Prazan konstruktor za inicijalizaciju.
        /// </summary>
        public Projekt2ViewModel() { }


        /// <summary>
        /// Konstruktor koji prima projekat.
        /// </summary>
        /// <param name="projekt">Projekt.</param>
        public Projekt2ViewModel(Projekt projekt)
        {
            this.Projekt = projekt;
            this.Dokumentacija = projekt.Dokumentacija?.ToList();
        }

        /// <summary>
        /// Kolekcija projekata.
        /// </summary>
        public IEnumerable<Projekt> Projekti { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }

        /// <summary>
        /// Lista dokumentacija.
        /// </summary>
        public List<Dokumentacija> Dokumentacija { get; set; }

        /// <summary>
        /// Objekt koji predstavlja projekt.
        /// </summary>
        public Projekt Projekt { get; set; }
    }
}
