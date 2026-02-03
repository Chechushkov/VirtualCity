using System.ComponentModel.DataAnnotations.Schema;

namespace Excursion_GPT.Domain.Entities;

[Table("tracks")]
public class Track
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("creatorid")]
    public Guid CreatorId { get; set; } // User who created this track

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<Point> Points { get; set; } = new List<Point>();
    public ICollection<Model> Models { get; set; } = new List<Model>(); // Models associated with this track
}