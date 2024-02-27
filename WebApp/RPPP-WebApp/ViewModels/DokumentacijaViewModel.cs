using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o dokumentaciji za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class DokumentacijaViewModel
    {
        /// <summary>
        /// Jedinstveni identifikator dokumenta.
        /// </summary>
        public int IdDok { get; set; }

        /// <summary>
        /// Ime dokumenta.
        /// </summary>
        public string ImeDok { get; set; }

        /// <summary>
        /// Jedinstveni identifikator projekta povezanog s ovim dokumentom.
        /// </summary>
        public int IdProjekta { get; set; }

        /// <summary>
        /// Jedinstveni identifikator vrste povezane s ovim dokumentom.
        /// </summary>
        public int IdVrste { get; set; }

        /// <summary>
        /// Ime projekta povezanog s ovim dokumentom.
        /// </summary>
        public string imeProjekta { get; set; }

        /// <summary>
        /// Ime vrste dokumenta.
        /// </summary>
        public string imeVrste { get; set; }

    }

}
