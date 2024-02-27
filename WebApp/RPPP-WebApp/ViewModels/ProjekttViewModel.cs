using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o projektima za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class ProjekttViewModel
    {
        /// <summary>
        /// Konstruktor koji prima kolekciju projekata.
        /// </summary>
        /// <param name="projekti">Kolekcija projekata.</param>
        public ProjekttViewModel(IEnumerable<Projekt> projekti)
        {
            this.Projekti = projekti;
        }
        /// <summary>
        /// Prazan konstruktor za inicijalizaciju.
        /// </summary>
        public ProjekttViewModel() { }
        /// <summary>
        /// Funkcija koja vrača kolekciju projekata koji sadrže ključnu riječ
        /// </summary>
        /// <param name="term">Riječ po kojoj filtiramo.</param>

        public IEnumerable<Projekt> FilteredProjekts(string term)
        {
            return Projekti.Where(p => p.ImeProjekta.Contains(term));
        }

        /// <summary>
        /// Konstruktor koji prima projekat.
        /// </summary>
        /// <param name="projekt">Projekt.</param>
        public ProjekttViewModel(Projekt projekt)
        {
            this.Projekt = projekt;
            this.Dokumentacija = projekt.Dokumentacija?.ToList(); // Check for null before calling ToList()
        }
        /// <summary>
        /// Kolekcija ViewModela za prikaz podataka o projektima.
        /// </summary>
        public IEnumerable<ProjektiiViewModel> projekti { get; set; }
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
