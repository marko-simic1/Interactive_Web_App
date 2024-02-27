using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o vrsta posla za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class VrstaPoslaViewModel
    {
        /// <summary>
        /// Konstruktor koji inicijalizira ViewModel s vrstama posla.
        /// </summary>
        /// <param name="vrstaPosla">Enumeracija vrste posla.</param>
        public VrstaPoslaViewModel(IEnumerable<VrstaPosla> vrstaPosla)
        {
            this.VrstePosla = vrstaPosla;
        }
        /// <summary>
        /// Konstruktor bez parametara.
        /// </summary>
        public VrstaPoslaViewModel() { }
        /// <summary>
        /// Enumeracija ViewModela vrste poslova.
        /// </summary>
        public IEnumerable<VrstePoslovaViewModel> vrste { get; set; }
        /// <summary>
        /// Enumeracija vrste poslova.
        /// </summary>
        public IEnumerable<VrstaPosla> VrstePosla { get; set; }
        /// <summary>
        /// Informacije o straničenju podataka.
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
        /// <summary>
        /// Podaci o vrsti posla.
        /// </summary>
        public VrstaPosla VrstaPosla { get; set; }
    }
}
