namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o vrsti projekta za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class VrstaProjektaViewModel
    {
        /// <summary>
        /// Jedinstveni identifikator vrste projekta.
        /// </summary>
        public int IdVrsteProjekta { get; set; }

        /// <summary>
        /// Ime vrste projekta.
        /// </summary>
        public string NazivVrsteProjekta { get; set; }
    }
}
