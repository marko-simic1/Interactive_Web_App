using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    public class JZadatakViewModel
    {
        public int IdZadatka { get; set; }
        public string Naslov { get; set; }
        //public  Zahtjev IdZahtjevaNavigation { get; set; }
        public DateTime PlanPocetak { get; set; }
        public DateTime PlanKraj { get; set; }
        public string Prioritet { get; set; }
    }
}
