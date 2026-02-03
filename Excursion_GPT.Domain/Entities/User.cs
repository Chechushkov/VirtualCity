using System.ComponentModel.DataAnnotations.Schema;
using Excursion_GPT.Domain.Enums;

namespace Excursion_GPT.Domain.Entities;

[Table("users")]
public class User
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("login")]
    public string Login { get; set; } = string.Empty;
    [Column("passwordhash")]
    public string PasswordHash { get; set; } = string.Empty;
    [Column("phone")]
    public string Phone { get; set; } = string.Empty;
    [Column("schoolname")]
    public string SchoolName { get; set; } = string.Empty;
    [Column("role")]
    public Role Role { get; set; }

    // Navigation properties
    public ICollection<Track> CreatedTracks { get; set; } = new List<Track>();
}