using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System;
using System.Drawing.Printing;

namespace RPPP_WebApp.ViewModels
{
    public class ZahtjevViewModel
    {

        public ZahtjevViewModel() { }

        public ZahtjevViewModel(ICollection<Zahtjev> zahtjevi)
        {
            this.Zahtjevi = zahtjevi;
        }

        public ZahtjevViewModel(Zahtjev zahtjev, ICollection<Projekt> projekti, ICollection<VrstaZah> vrstaZah)
        {
            this.Zahtjev = zahtjev;
            this.Projekti = projekti;
            this.VrstaZahtjeva = vrstaZah;
        }

        public ZahtjevViewModel(ICollection<Projekt> projekti, ICollection<VrstaZah> zahtjevi)
        {
            this.Projekti = projekti;
            this.VrstaZahtjeva = zahtjevi;
        }

        public ZahtjevViewModel(ICollection<VrstaZah> vrstaZahtjeva)
        {
            this.VrstaZahtjeva = vrstaZahtjeva;
        }

        public ZahtjevViewModel(VrstaZah vrstaZahtjev)
        {
            this.VrstaZahtjev = vrstaZahtjev;
        }

        public ICollection<Zahtjev> Zahtjevi { get; set; }
        public Zahtjev Zahtjev { get; set; }
        public ICollection<Projekt> Projekti { get; set; }
        public ICollection<VrstaZah> VrstaZahtjeva { get; set; }
        public VrstaZah VrstaZahtjev { get; set; }

        public PagingInfo PagingInfo { get; set; }

    }

}