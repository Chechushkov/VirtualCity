using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Excursion_GPT.Domain.Entities;

[Table("models")]
public class Model
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("buildingid")]
    public Guid BuildingId { get; set; }
    [Column("trackid")]
    public Guid TrackId { get; set; }
    [Column("minioobjectname")]
    public string MinioObjectName { get; set; } = string.Empty; // Name in MinIO bucket
    [Column("position")]
    public List<double> Position { get; set; } = new List<double> { 0, 0, 0 }; // [x, y, z]
    [Column("rotation")]
    public List<double> Rotation { get; set; } = new List<double> { 0, 0, 0 }; // [a, b, c]
    [Column("scale")]
    public double Scale { get; set; }

    // Navigation properties
    public Building Building { get; set; } = null!;
    public Track Track { get; set; } = null!;
}
