namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o osobama za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class OsobeViewModels
    {
        /// <summary>
        /// Identifikator osobe.
        /// </summary>
        public int IdOsoba { get; set; }

        /// <summary>
        /// Ime osobe.
        /// </summary>
        public string Ime { get; set; }

        /// <summary>
        /// Prezime osobe.
        /// </summary>
        public string Prezime { get; set; }

        /// <summary>
        /// IBAN (International Bank Account Number) osobe.
        /// </summary>
        public string Iban { get; set; }

        /// <summary>
        /// E-mail adresa osobe.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Telefon osobe.
        /// </summary>
        public string Telefon { get; set; }
    }
}
