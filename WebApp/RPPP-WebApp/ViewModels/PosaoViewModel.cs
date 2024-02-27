using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o poslovima za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class PosaoViewModel
    {
        /// <summary>
        /// Konstruktor koji prima kolekciju poslova.
        /// </summary>
        /// <param name="poslovi">Kolekcija poslova.</param>
        public PosaoViewModel(IEnumerable<Posao> poslovi)
        {
            this.Poslovi = poslovi;
        }
        /// <summary>
        /// Prazan konstruktor za inicijalizaciju.
        /// </summary>
        public PosaoViewModel() { }

        /// <summary>
        /// Konstruktor koji prima jedan posao
        /// </summary>
        /// <param name="posao">Posao.</param>
        public PosaoViewModel(Posao posao)
        {
            this.Posao = posao;
            this.Osobe = posao.IdOsoba?.ToList() ?? new List<Osoba>(); // Check for null before calling ToList()
        }
        /// <summary>
        /// Kolekcija ViewModela za prikaz podataka o poslovima.
        /// </summary>
        public IEnumerable<PosloviViewModel> poslovi { get; set; }
        /// <summary>
        /// Kolekcija poslova.
        /// </summary>
        public IEnumerable<Posao> Poslovi { get; set; }
        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
        /// <summary>
        /// Kolekcija osoba.
        /// </summary>
        public List<Osoba> Osobe { get; set; }

        /// <summary>
        /// Objekt koji predstavlja posao.
        /// </summary>
        public Posao Posao { get; set; }
        /// <summary>
        /// Objekt koji predstavlja vrstu posla.
        /// </summary>
        public VrstaPosla VrstaPosla { get; set; }
    }
}