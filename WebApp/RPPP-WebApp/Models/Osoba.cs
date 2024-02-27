#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models
{
    /// <summary>
    /// Entitet koji predstavlja osobu.
    /// </summary>
    [Table("osoba")]
    public partial class Osoba
    {
        /// <summary>
        /// Ime osobe.
        /// </summary>
        [Required]
        [Column("ime", TypeName = "text")]
        public string Ime { get; set; }

        /// <summary>
        /// Jedinstveni identifikator osobe.
        /// </summary>
        [Key]
        [Column("idOsoba")]
        public int IdOsoba { get; set; }

        /// <summary>
        /// E-mail adresa osobe.
        /// </summary>
        [Required]
        [Column("email", TypeName = "text")]
        public string Email { get; set; }

        /// <summary>
        /// Telefon osobe.
        /// </summary>
        [Column("telefon")]
        public string Telefon { get; set; }

        /// <summary>
        /// IBAN (Međunarodni bankovni račun) osobe.
        /// </summary>
        [Column("IBAN", TypeName = "text")]
        public string Iban { get; set; }

        /// <summary>
        /// Prezime osobe.
        /// </summary>
        [Required]
        [Column("prezime", TypeName = "text")]
        public string Prezime { get; set; }

        /// <summary>
        /// Jedinstveni identifikator partnera povezanog s ovom osobom.
        /// </summary>
        [Column("idPartnera")]
        public int? IdPartnera { get; set; }

        /// <summary>
        /// Navigacijski svojstvo koje predstavlja partnera povezanog s ovom osobom.
        /// </summary>
        [ForeignKey("IdPartnera")]
        [InverseProperty("Osoba")]
        public virtual Partner IdPartneraNavigation { get; set; }

        /// <summary>
        /// Kolekcija radnih mjesta na kojima osoba radi.
        /// </summary>
        [InverseProperty("IdOsobaNavigation")]
        public virtual ICollection<RadiNa> RadiNa { get; set; } = new List<RadiNa>();

        /// <summary>
        /// Kolekcija zadataka koje osoba izvršava.
        /// </summary>
        [InverseProperty("IdOsobaNavigation")]
        public virtual ICollection<Zadatak> Zadatak { get; set; } = new List<Zadatak>();

        /// <summary>
        /// Kolekcija poslova povezanih s ovom osobom.
        /// </summary>
        [ForeignKey("IdOsoba")]
        [InverseProperty("IdOsoba")]
        public virtual ICollection<Posao> IdPosla { get; set; } = new List<Posao>();
    }
}
