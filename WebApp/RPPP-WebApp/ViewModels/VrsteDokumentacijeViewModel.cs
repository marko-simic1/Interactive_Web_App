namespace RPPP_WebApp.ViewModels
{
    public class VrsteDokumentacijeViewModel
    {

        /// <summary>
        /// Kolekcija vrsti dokumentacija.
        /// </summary>
        public IEnumerable<VrstaDokumentacijeViewModel> VrsteDokumentacija { get; set; }

        /// <summary>
        /// Informacije o straničenju (paging).
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}

