#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models
{
    /// <summary>
    /// Entitet koji predstavlja partnera.
    /// </summary>
    [Table("partner")]
    [Index("Oib", Name = "UQ__partner__CB394B3E95C7A891", IsUnique = true)]
    public partial class Partner
    {
        /// <summary>
        /// Jedinstveni identifikator partnera.
        /// </summary>
        [Key]
        [Column("idPartnera")]
        public int IdPartnera { get; set; }

        /// <summary>
        /// OIB (Osobni identifikacijski broj) partnera.
        /// </summary>
        [Column("OIB")]
        public int Oib { get; set; }

        /// <summary>
        /// E-mail adresa partnera.
        /// </summary>
        [Required]
        [Column("email", TypeName = "text")]
        public string Email { get; set; }

        /// <summary>
        /// Telefon partnera.
        /// </summary>
        [Column("telefon")]
        public int Telefon { get; set; }

        /// <summary>
        /// IBAN (Međunarodni bankovni račun) partnera.
        /// </summary>
        [Column("IBAN", TypeName = "text")]
        public string Iban { get; set; }

        /// <summary>
        /// Ime partnera.
        /// </summary>
        [Required]
        [Column("ime", TypeName = "text")]
        public string Ime { get; set; }

        /// <summary>
        /// Adresa partnera.
        /// </summary>
        [Column("adresa", TypeName = "text")]
        public string Adresa { get; set; }

        /// <summary>
        /// Kolekcija osoba povezanih s ovim partnerom.
        /// </summary>
        [InverseProperty("IdPartneraNavigation")]
        public virtual ICollection<Osoba> Osoba { get; set; } = new List<Osoba>();

        /// <summary>
        /// Kolekcija projekata povezanih s ovim partnerom.
        /// </summary>
        [ForeignKey("IdPartnera")]
        [InverseProperty("IdPartnera")]
        public virtual ICollection<Projekt> IdProjekta { get; set; } = new List<Projekt>();
    }
}
