namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o vrsti posla za prikaz na korisničkom sučelju.
    /// </summary>
    public class VrstePoslovaViewModel
    {
        /// <summary>
        /// Jedinstevni identifikator vrste posla.
        /// </summary>
        public int IdVrstePosla { get; set; }
        /// <summary>
        /// Naziv vrste posla.
        /// </summary>
        public string NazivVrstePosla { get; set; }
    }
}