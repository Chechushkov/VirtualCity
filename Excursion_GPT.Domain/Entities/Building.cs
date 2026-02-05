using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace Excursion_GPT.Domain.Entities;

[Table("buildings")]
public class Building
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("x")]
    public double X { get; set; } // Web Mercator X coordinate (meters) - center point

    [Column("z")]
    public double Z { get; set; } // Web Mercator Z coordinate (meters) - center point

    [Column("address")]
    public string? Address { get; set; } // Building address

    [Column("height")]
    public double? Height { get; set; } // Building height in meters

    [Column("modelid")]
    public Guid? ModelId { get; set; } // Reference to custom model if exists

    [Column("rotation")]
    public List<double>? Rotation { get; set; } // [x, y, z] if custom model

    [Column("nodes_json")]
    public string? NodesJson { get; set; } // Polygon nodes as JSON string

    // Navigation properties
    public Model? CustomModel { get; set; }

    // Property to get/set nodes as List<List<double>>
    // IMPORTANT: This property is NOT mapped to database - it's computed from NodesJson
    // Entity Framework should ignore this property completely
    [NotMapped]
    [System.Text.Json.Serialization.JsonIgnore]
    public List<List<double>>? Nodes
    {
        get
        {
            if (string.IsNullOrEmpty(NodesJson))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<List<double>>>(NodesJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            if (value == null)
            {
                NodesJson = null;
            }
            else
            {
                NodesJson = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
    }
}
