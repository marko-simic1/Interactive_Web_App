﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Models;

[PrimaryKey("IdProjekta", "IdOsoba", "IdUloge")]
[Table("radiNa")]
public partial class RadiNa
{
    [Key]
    [Column("idProjekta")]
    public int IdProjekta { get; set; }

    [Key]
    [Column("idOsoba")]
    public int IdOsoba { get; set; }

    [Key]
    [Column("idUloge")]
    public int IdUloge { get; set; }

    [ForeignKey("IdOsoba")]
    [InverseProperty("RadiNa")]
    public virtual Osoba IdOsobaNavigation { get; set; }

    [ForeignKey("IdProjekta")]
    [InverseProperty("RadiNa")]
    public virtual Projekt IdProjektaNavigation { get; set; }

    [ForeignKey("IdUloge")]
    [InverseProperty("RadiNa")]
    public virtual Uloga IdUlogeNavigation { get; set; }
}