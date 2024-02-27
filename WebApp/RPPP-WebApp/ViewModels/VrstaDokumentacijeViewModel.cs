namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o vrsti dokumentacije za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class VrstaDokumentacijeViewModel
    {
        /// <summary>
        /// Jedinstveni identifikator vrste dokumenta.
        /// </summary>
        public int IdVrsteDokumentacije { get; set; }

        /// <summary>
        /// Ime vrste dokumenta.
        /// </summary>
        public string NazivVrsteDokumentacije { get; set; }
    }
}
