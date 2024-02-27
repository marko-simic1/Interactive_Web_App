#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models;

/// <summary>
/// Entitet koji predstavlja projekt.
/// </summary>
[Table("projekt")]
public partial class Projekt
{
    /// <summary>
    /// Jedinstveni identifikator projekta.
    /// </summary>
    [Key]
    [Column("idProjekta")]
    public int IdProjekta { get; set; }

    /// <summary>
    /// Kratica projekta.
    /// </summary>
    [Required]
    [Column("kratica", TypeName = "text")]
    public string Kratica { get; set; }

    /// <summary>
    /// Sažetak projekta.
    /// </summary>
    [Column("sazetak", TypeName = "text")]
    public string Sazetak { get; set; }

    /// <summary>
    /// Ime projekta.
    /// </summary>
    [Required]
    [Column("imeProjekta", TypeName = "text")]
    public string ImeProjekta { get; set; }

    /// <summary>
    /// Datum početka projekta.
    /// </summary>
    [Column("datumPoc", TypeName = "date")]
    public DateTime? DatumPoc { get; set; }

    /// <summary>
    /// Datum završetka projekta.
    /// </summary>
    [Column("datumZav", TypeName = "date")]
    public DateTime? DatumZav { get; set; }

    /// <summary>
    /// Broj kartice projekta.
    /// </summary>
    [Column("brKartice")]
    public int? BrKartice { get; set; }

    /// <summary>
    /// Jedinstveni identifikator vrste povezane s ovim projektom.
    /// </summary>
    [Column("idVrste")]
    public int IdVrste { get; set; }

    /// <summary>
    /// Navigacijsko svojstvo koje predstavlja broj kartice povezan s ovim projektom.
    /// </summary>
    [ForeignKey("BrKartice")]
    [InverseProperty("Projekt")]
    public virtual Kartica BrKarticeNavigation { get; set; }

    /// <summary>
    /// Kolekcija dokumentacija povezanih s ovim projektom.
    /// </summary>
    [InverseProperty("IdProjektaNavigation")]
    public virtual ICollection<Dokumentacija> Dokumentacija { get; set; } = new List<Dokumentacija>();

    /// <summary>
    /// Navigacijsko svojstvo koje predstavlja vrstu povezanu s ovim projektom.
    /// </summary>
    [ForeignKey("IdVrste")]
    [InverseProperty("Projekt")]
    public virtual VrstaProjekta IdVrsteNavigation { get; set; }

    /// <summary>
    /// Kolekcija radiNa povezanih s ovim projektom.
    /// </summary>
    [InverseProperty("IdProjektaNavigation")]
    public virtual ICollection<RadiNa> RadiNa { get; set; } = new List<RadiNa>();

    /// <summary>
    /// Kolekcija zahtjeva povezanih s ovim projektom.
    /// </summary>
    [InverseProperty("IdProjektaNavigation")]
    public virtual ICollection<Zahtjev> Zahtjev { get; set; } = new List<Zahtjev>();

    /// <summary>
    /// Kolekcija partnera povezanih s ovim projektom.
    /// </summary>
    [ForeignKey("IdProjekta")]
    [InverseProperty("IdProjekta")]
    public virtual ICollection<Partner> IdPartnera { get; set; } = new List<Partner>();
}