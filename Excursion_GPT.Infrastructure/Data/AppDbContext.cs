using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Excursion_GPT.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        _options = options;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress the pending model changes warning to allow initial migration
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<Building> Buildings { get; set; } = null!;
    public virtual DbSet<Model> Models { get; set; } = null!;
    public virtual DbSet<Track> Tracks { get; set; } = null!;
    public virtual DbSet<Point> Points { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Login).IsUnique();
            entity.Property(u => u.Role)
                  .HasConversion<string>(); // Store enum as string
        });

        // Building configuration
        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Rotation)
                  .HasColumnType("jsonb"); // Store double array as JSONB
            entity.HasOne(b => b.CustomModel)
                  .WithOne()
                  .HasForeignKey<Building>(b => b.ModelId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull); // If model is deleted, unlink from building
        });

        // Model configuration
        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Position)
                  .HasColumnType("jsonb"); // Store double array as JSONB
            entity.Property(m => m.Rotation)
                  .HasColumnType("jsonb"); // Store double array as JSONB
            entity.HasOne(m => m.Building)
                  .WithMany()
                  .HasForeignKey(m => m.BuildingId)
                  .OnDelete(DeleteBehavior.Cascade); // If building deleted, delete model
            entity.HasOne(m => m.Track)
                  .WithMany(t => t.Models)
                  .HasForeignKey(m => m.TrackId)
                  .OnDelete(DeleteBehavior.Cascade); // If track deleted, delete model
        });

        // Track configuration
        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasOne(t => t.Creator)
                  .WithMany(u => u.CreatedTracks)
                  .HasForeignKey(t => t.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete user if they created tracks
        });

        // Point configuration
        modelBuilder.Entity<Point>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Position)
                  .HasColumnType("jsonb"); // Store double array as JSONB
            entity.Property(p => p.Rotation)
                  .HasColumnType("jsonb"); // Store double array as JSONB
            entity.HasOne(p => p.Track)
                  .WithMany(t => t.Points)
                  .HasForeignKey(p => p.TrackId)
                  .OnDelete(DeleteBehavior.Cascade); // If track deleted, delete points
        });


    }
    /*
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                var connectionLoyCreator = _options.
                optionsBuilder.UseNpgsql(connectionLoyCreator);
            }
    */
}
