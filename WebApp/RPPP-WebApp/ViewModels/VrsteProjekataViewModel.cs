namespace RPPP_WebApp.ViewModels
{
    public class VrsteProjekataViewModel
    {
        /// <summary>
        /// Kolekcija vrsti projekata.
        /// </summary>
        public IEnumerable<VrstaProjektaViewModel> VrsteProjekta { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}
