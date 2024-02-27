namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o ulogama za prikaz na korisničkom sučelju.
    /// </summary>
    public class UlogeViewModel
    {
        /// <summary>
        /// Identifikator uloge.
        /// </summary>
        public int IdUloge { get; set; }

        /// <summary>
        /// Naziv uloge.
        /// </summary>
        public string ImeUloge { get; set; }

        /// <summary>
        /// Opis uloge.
        /// </summary>
        public string Opis { get; set; }

        /// <summary>
        /// Identifikator vrste uloge.
        /// </summary>
        public int IdVrste { get; set; }
    }
}
