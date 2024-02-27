namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o partnerima za prikaz na korisničkom sučelju.
    /// </summary>
    public class PartneriViewModel
    {
        /// <summary>
        /// OIB partnera.
        /// </summary>
        public int Oib { get; set; }

        /// <summary>
        /// Email partnera.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Telefon partnera.
        /// </summary>
        public int Telefon { get; set; }

        /// <summary>
        /// IBAN partnera.
        /// </summary>
        public string Iban { get; set; }

        /// <summary>
        /// Ime partnera.
        /// </summary>
        public string Ime { get; set; }

        /// <summary>
        /// Adresa partnera.
        /// </summary>
        public string Adresa { get; set; }

        /// <summary>
        /// Id partnera.
        /// </summary>
        public int IdPartnera { get; set; }
    }
}
