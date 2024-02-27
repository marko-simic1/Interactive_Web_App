using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel za prikaz entiteta transakcije u retku tablcie
    /// </summary>
    public class TransakcijeViewModel
    {
        /// <summary>
        /// Model transakcije
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// Poziv na broj
        /// </summary>
        public int? PozivNaBr { get; set; }
        /// <summary>
        /// ID transakcije
        /// </summary>
        public int IdTrans { get; set; }
        /// <summary>
        /// Opis transakcije
        /// </summary>
        public string Opis { get; set; }
        /// <summary>
        /// Iznos transakcije
        /// </summary>
        public decimal Iznos { get; set; }
        /// <summary>
        /// Datum transakcije
        /// </summary>
        public DateTime? Datum { get; set; }
        /// <summary>
        /// Vrsta transakcije uz koju je transakcija vezana
        /// </summary>
        public virtual VrstaTransakcije IdVrsteNavigation { get; set; }
        /// <summary>
        /// Broj kartice
        /// </summary>
        public int BrKartice { get; set; }
    }
}
