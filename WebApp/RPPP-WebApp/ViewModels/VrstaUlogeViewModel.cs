using RPPP_WebApp.Models;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji predstavlja podatke o vrsti uloge za prikaz na korisničkom sučelju.
    /// </summary>
    public class VrstaUlogeViewModel
    {
        /// <summary>
        /// Konstruktor koji inicijalizira ViewModel s kolekcijom vrsta uloga.
        /// </summary>
        /// <param name="vrstaUloge">Kolekcija vrsta uloga.</param>
        public VrstaUlogeViewModel(IEnumerable<VrstaUloge> vrstaUloge)
        {
            this.VrsteUloga = vrstaUloge;
        }

        /// <summary>
        /// Prazan konstruktor za ViewModel.
        /// </summary>
        public VrstaUlogeViewModel() { }

        /// <summary>
        /// Kolekcija ViewModela za pojedine vrste uloga.
        /// </summary>
        public IEnumerable<VrsteUlogaViewModel> vrste { get; set; }

        /// <summary>
        /// Kolekcija svih vrsta uloga.
        /// </summary>
        public IEnumerable<VrstaUloge> VrsteUloga { get; set; }

        /// <summary>
        /// Informacije o straničenju za prikaz kolekcije vrsta uloga.
        /// </summary>
        public PagingInfo PagingInfo { get; set; }

        /// <summary>
        /// Podaci o odabranoj vrsti uloge.
        /// </summary>
        public VrstaUloge VrstaUloge { get; set; }
    }
}
