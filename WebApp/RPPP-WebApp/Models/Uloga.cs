#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models
{
    /// <summary>
    /// Entitet koji predstavlja ulogu.
    /// </summary>
    [Table("uloga")]
    public partial class Uloga
    {
        /// <summary>
        /// Jedinstveni identifikator uloge.
        /// </summary>
        [Key]
        [Column("idUloge")]
        public int IdUloge { get; set; }

        /// <summary>
        /// Ime uloge.
        /// </summary>
        [Required]
        [Column("imeUloge", TypeName = "text")]
        public string ImeUloge { get; set; }

        /// <summary>
        /// Opis uloge.
        /// </summary>
        [Column("opis", TypeName = "text")]
        public string Opis { get; set; }

        /// <summary>
        /// Jedinstveni identifikator vrste uloge kojoj pripada ova uloga.
        /// </summary>
        [Column("idVrste")]
        public int IdVrste { get; set; }

        /// <summary>
        /// Navigacijsko svojstvo koje predstavlja vrstu uloge kojoj pripada ova uloga.
        /// </summary>
        [ForeignKey("IdVrste")]
        [InverseProperty("Uloga")]
        public virtual VrstaUloge IdVrsteNavigation { get; set; }

        /// <summary>
        /// Kolekcija radnih mjesta na kojima je ova uloga dodijeljena.
        /// </summary>
        [InverseProperty("IdUlogeNavigation")]
        public virtual ICollection<RadiNa> RadiNa { get; set; } = new List<RadiNa>();
    }
}
