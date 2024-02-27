namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o projektu za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class ProjektiiViewModel
    {
        /// <summary>
        /// Jedinstveni identifikator projekta.
        /// </summary>
        public int IdProjekta { get; set; }
        /// <summary>
        /// Ime projekta.
        /// </summary>
        public string ImeProjekta { get; set; }
        /// <summary>
        /// Kratica projekta.
        /// </summary>
        public string Kratica { get; set; }
        /// <summary>
        /// Sažetak projekta.
        /// </summary>
        public string Sazetak { get; set; }
        /// <summary>
        /// Datum početka projekta.
        /// </summary>
        public DateTime? DatumPoc { get; set; }
        /// <summary>
        /// Datum završetka projekta.
        /// </summary>
        public DateTime? DatumZav { get; set; }
        /// <summary>
        /// Broj kartice projekta.
        /// </summary>
        public int? BrKartice { get; set; }
        /// <summary>
        /// Jedinstveni identifikator vrste povezane s ovim projektom.
        /// </summary>
        public int IdVrste { get; set; }
        /// <summary>
        /// Ime vrste projekta.
        /// </summary>
        public string imeVrste { get; set; }
    }
}


