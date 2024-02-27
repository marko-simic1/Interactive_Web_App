namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o projektu za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class ProjektiViewModel
    {
        /// <summary>
        /// Kolekcija projekata.
        /// </summary>
        public IEnumerable<ProjektViewModel> Projekti { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}
