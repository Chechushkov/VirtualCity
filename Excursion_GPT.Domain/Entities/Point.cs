using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Excursion_GPT.Domain.Entities;

[Table("points")]
public class Point
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("trackid")]
    public Guid TrackId { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("type")]
    public string Type { get; set; } = string.Empty; // e.g., "start", "checkpoint", "end"
    [Column("position")]
    public List<double> Position { get; set; } = new List<double> { 0, 0, 0 }; // [x, y, z]
    [Column("rotation")]
    public List<double> Rotation { get; set; } = new List<double> { 0, 0, 0 }; // [a, b, c]

    // Navigation properties
    public Track Track { get; set; } = null!;
}
