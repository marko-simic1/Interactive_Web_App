using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel entiteta kartica za prikaz u tablici
    /// </summary>
    public class KarticeViewModel
    {
        /// <summary>
        /// Broj kartice
        /// </summary>
        public int BrKartice { get; set; }
        /// <summary>
        /// Stanje kartice
        /// </summary>
        public decimal Stanje { get; set; }
        /// <summary>
        /// Projekt vezan uz karticu
        /// </summary>
        public virtual ICollection<Projekt> Projekt { get; set; }

    }
}
