using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// ViewModel koji sadrži podatke o partnerima za prikaz i manipulaciju na korisničkom sučelju.
    /// </summary>
    public class PartnerViewModel
    {
        /// <summary>
        /// Filtrira partnere prema zadanim kriterijima.
        /// </summary>
        /// <param name="term">Tekstualni pojam prema kojem se filtriraju partneri.</param>
        /// <returns>Enumeracija filtriranih partnera.</returns>
        public IEnumerable<Partner> FilteredPartners(string term)
        {
            return Partneri.Where(p => p.Ime.Contains(term));
        }

        /// <summary>
        /// Konstruktor koji inicijalizira ViewModel s partnerima.
        /// </summary>
        /// <param name="partneri">Enumeracija partnera.</param>
        public PartnerViewModel(IEnumerable<Partner> partneri)
        {
            this.Partneri = partneri;
        }

        /// <summary>
        /// Konstruktor bez parametara.
        /// </summary>
        public PartnerViewModel() { }

        /// <summary>
        /// Enumeracija ViewModela partnera.
        /// </summary>
        public IEnumerable<PartneriViewModel> partneri { get; set; }

        /// <summary>
        /// Enumeracija projekata.
        /// </summary>
        public IEnumerable<Projekt> projekti { get; set; }

        /// <summary>
        /// Podaci o partneru.
        /// </summary>
        public Partner Partner { get; set; }

        /// <summary>
        /// Enumeracija partnera.
        /// </summary>
        public IEnumerable<Partner> Partneri { get; set; }

        /// <summary>
        /// Informacije o straničenju podataka.
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}
