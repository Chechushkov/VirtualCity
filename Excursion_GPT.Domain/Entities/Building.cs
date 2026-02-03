using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Excursion_GPT.Domain.Entities;

[Table("buildings")]
public class Building
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("latitude")]
    public double Latitude { get; set; }
    [Column("longitude")]
    public double Longitude { get; set; }
    [Column("modelid")]
    public Guid? ModelId { get; set; } // Reference to custom model if exists
    [Column("rotation")]
    public List<double>? Rotation { get; set; } // [x, y, z] if custom model

    // Navigation properties
    public Model? CustomModel { get; set; }
}
