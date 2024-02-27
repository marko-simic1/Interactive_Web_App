using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel za entitet Transakcije
    /// </summary>
    public class TransakcijaViewModel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="transakcije">Lista transakcija</param>
        public TransakcijaViewModel(IEnumerable<Transakcija> transakcije)
        {
            this.Transakcije = transakcije;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TransakcijaViewModel() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="transakcija">Objekt transakcije</param>
        public TransakcijaViewModel(Transakcija transakcija) { this.Transakcija = transakcija; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="transakcija">Objekt transakcije</param>
        /// <param name="kartice">Lista kartica</param>
        /// <param name="vrste">Lista vrsta transakcija</param>
        public TransakcijaViewModel(Transakcija transakcija, IEnumerable<Kartica> kartice, IEnumerable<VrstaTransakcije> vrste)
        {
            this.Transakcija = transakcija;
            this.Kartice = kartice;
            this.Vrste = vrste;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="transakcija">Objekt transakcije</param>
        /// <param name="kartice">Lista kartica</param>
        /// <param name="vrste">Lista vrsta transakcija</param>
        /// <param name="redirect">Redirekt na prošlu stranicu</param>
        public TransakcijaViewModel(Transakcija transakcija, IEnumerable<Kartica> kartice, IEnumerable<VrstaTransakcije> vrste, int redirect)
        {
            this.Transakcija = transakcija;
            this.Kartice = kartice;
            this.Vrste = vrste;
            this.Redirect = redirect;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="transakcija">Objekt transakcije</param>
        /// <param name="kartice">Lista kartica</param>
        public TransakcijaViewModel(Transakcija transakcija, IEnumerable<Kartica> kartice)
        {
            this.Transakcija = transakcija;
            this.Kartice = kartice;
        }
        /// <summary>
        /// Lista transakcija
        /// </summary>
        public IEnumerable<Transakcija> Transakcije { get; set; }
        /// <summary>
        /// Lista TransakcijeViewModela
        /// </summary>
        public IEnumerable<TransakcijeViewModel> transakcije { get; set; }
        /// <summary>
        /// Lista kartica
        /// </summary>
        public IEnumerable<Kartica> Kartice { get; set; }
        /// <summary>
        /// Lista vrsta transakcija
        /// </summary>
        public IEnumerable<VrstaTransakcije> Vrste { get; set; }
        /// <summary>
        /// Objekt transakcije
        /// </summary>
        public Transakcija Transakcija { get; set; }
        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
        /// <summary>
        /// Redirekt na stranicu
        /// </summary>
        public int Redirect { get; set; }
    }
}
