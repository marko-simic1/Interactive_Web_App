using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.ViewModels
{
    public class ZadaciViewModel
    {
        public ZadaciViewModel() { }
        public ZadaciViewModel(ICollection<Zadatak> Zadaci)
        {
            this.Zadaci = Zadaci;
        }

        public ZadaciViewModel(Zadatak Zadatak, ICollection<Zahtjev> Zahtjevi, ICollection<Osoba> Osobe, ICollection<Status> Statusi)
        {
            this.Zadatak = Zadatak;
            this.Zahtjevi = Zahtjevi;
            this.Osobe = Osobe;
            this.Statusi = Statusi;
        }

        public ZadaciViewModel(int? id, Zadatak Zadatak, ICollection<Zahtjev> Zahtjevi, ICollection<Osoba> Osobe, ICollection<Status> Statusi)
        {
            this.Zadatak = Zadatak;
            this.Zahtjevi = Zahtjevi;
            this.Osobe = Osobe;
            this.Statusi = Statusi;
            this.Id = id;
        }

        public int? Id { get; set; }
        public ICollection<Zadatak> Zadaci { get; set; }
        public Zadatak Zadatak { get; set; }
        public ICollection<Zahtjev> Zahtjevi { get; set; }
        public ICollection<Osoba> Osobe { get; set; }
        public ICollection<Status> Statusi { get; set; }
        public PagingInfo PagingInfo { get; set; }


    }
}