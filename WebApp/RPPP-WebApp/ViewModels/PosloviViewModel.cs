
namespace RPPP_WebApp.ViewModels

{
    /// <summary>
    /// ViewModel koji sadrži podatke o osobi za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class PosloviViewModel
    {
        /// <summary>
        /// Identifikator posla.
        /// </summary>
        public int IdPosla { get; set; }
        /// <summary>
        /// Opis posla.
        /// </summary>
        public string Opis { get; set; }
        /// <summary>
        /// Ime posla.
        /// </summary>
        public string ImePosla { get; set; }
        /// <summary>
        /// Jedinstveni identifikator vrste posla.
        /// </summary>
        public int IdVrste { get; set; }
        /// <summary>
        /// Ime vrste posla.
        /// </summary>
        public string ImeVrste { get; set; }

    }
}

