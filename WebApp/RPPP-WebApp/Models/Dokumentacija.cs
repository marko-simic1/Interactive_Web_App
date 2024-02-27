#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models;

/// <summary>
/// Entitet koji predstavlja dokumentaciju.
/// </summary>
[PrimaryKey("IdDok")]
[Table("dokumentacija")]
public partial class Dokumentacija
{
    /// <summary>
    /// Ime dokumenta.
    /// </summary>
    [Required]
    [Column("imeDok", TypeName = "text")]
    public string ImeDok { get; set; }

    /// <summary>
    /// Jedinstveni identifikator dokumenta.
    /// </summary>
    [Key]
    [Column("idDok")]
    public int IdDok { get; set; }

    /// <summary>
    /// Jedinstveni identifikator projekta povezanog s ovim dokumentom.
    /// </summary>
    [Column("idProjekta")]
    public int IdProjekta { get; set; }

    /// <summary>
    /// Jedinstveni identifikator vrste povezane s ovim dokumentom.
    /// </summary>
    [Column("idVrste")]
    public int IdVrste { get; set; }

    /// <summary>
    /// Navigacijsko svojstvo koje predstavlja projekt povezanog s ovim dokumentom.
    /// </summary>
    [ForeignKey("IdProjekta")]
    [InverseProperty("Dokumentacija")]
    public virtual Projekt IdProjektaNavigation { get; set; }

    /// <summary>
    /// Navigacijsko svojstvo koje predstavlja vrstu povezanu s ovim dokumentom.
    /// </summary>
    [ForeignKey("IdVrste")]
    [InverseProperty("Dokumentacija")]
    public virtual VrstaDok IdVrsteNavigation { get; set; }
}