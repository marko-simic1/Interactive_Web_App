#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models
{
    /// <summary>
    /// Entitet koji predstavlja vrstu uloge.
    /// </summary>
    [Table("vrstaUloge")]
    public partial class VrstaUloge
    {
        /// <summary>
        /// Jedinstveni identifikator vrste uloge.
        /// </summary>
        [Key]
        [Column("idVrste")]
        public int IdVrste { get; set; }

        /// <summary>
        /// Ime vrste uloge.
        /// </summary>
        [Required]
        [Column("imeVrste", TypeName = "text")]
        public string ImeVrste { get; set; }

        /// <summary>
        /// Kolekcija uloga koje pripadaju ovoj vrsti uloge.
        /// </summary>
        [InverseProperty("IdVrsteNavigation")]
        public virtual ICollection<Uloga> Uloga { get; set; } = new List<Uloga>();
    }
}
