#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models;

/// <summary>
/// Entitet koji predstavlja vrstu dokumentacije.
/// </summary>
[Table("vrstaDok")]
public partial class VrstaDok
{
    /// <summary>
    /// Jedinstveni identifikator vrste dokumenta.
    /// </summary>
    [Key]
    [Column("idVrste")]
    public int IdVrste { get; set; }

    /// <summary>
    /// Ime vrste dokumenta.
    /// </summary>
    [Required]
    [Column("imeVrste", TypeName = "text")]
    public string ImeVrste { get; set; }

    /// <summary>
    /// Kolekcija dokumenata povezanih s ovom vrstom dokumenta.
    /// </summary>
    [InverseProperty("IdVrsteNavigation")]
    public virtual ICollection<Dokumentacija> Dokumentacija { get; set; } = new List<Dokumentacija>();
}