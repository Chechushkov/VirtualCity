using System.ComponentModel.DataAnnotations.Schema;

namespace Excursion_GPT.Domain.Entities;

[Table("model_polygons")]
public class ModelPolygon
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("model_id")]
    public Guid ModelId { get; set; }

    [Column("polygon_id")]
    public Guid PolygonId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Model Model { get; set; } = null!;
    public Building Polygon { get; set; } = null!;
}
