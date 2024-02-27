using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel entiteta vrste transakcije
    /// </summary>
    public class VrstaTransakcijeViewModel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="vrste">Lista vrsti transakcija</param>
        public VrstaTransakcijeViewModel(IEnumerable<VrstaTransakcije> vrste)
        {
            this.Vrste = vrste;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public VrstaTransakcijeViewModel() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="vrsta">Objekt vrste transakcije</param>
        public VrstaTransakcijeViewModel(VrstaTransakcije vrsta) { this.Vrsta = vrsta; }
        /// <summary>
        /// Lista vrsti transakcija
        /// </summary>
        public IEnumerable<VrstaTransakcije> Vrste { get; set; }
        /// <summary>
        /// Lista VrsteTransakcijaViewModela
        /// </summary>
        public IEnumerable<VrsteTransakcijaViewModel> vrste { get; set; }
        /// <summary>
        /// Objekt vrste transakcije
        /// </summary>
        public VrstaTransakcije Vrsta { get; set; }
        /// <summary>
        /// Informacije o straničenju
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}
