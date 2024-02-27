#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models;

/// <summary>
/// Entitet koji predstavlja vrstu projekta.
/// </summary>
[Table("vrstaProjekta")]
public partial class VrstaProjekta
{
    /// <summary>
    /// Jedinstveni identifikator vrste projekta.
    /// </summary>
    [Key]
    [Column("idVrste")]
    public int IdVrste { get; set; }

    /// <summary>
    /// Ime vrste projekta.
    /// </summary>
    [Required]
    [Column("imeVrste", TypeName = "text")]
    public string ImeVrste { get; set; }

    /// <summary>
    /// Kolekcija projekata povezanih s ovom vrstom projekta.
    /// </summary>
    [InverseProperty("IdVrsteNavigation")]
    public virtual ICollection<Projekt> Projekt { get; set; } = new List<Projekt>();
}