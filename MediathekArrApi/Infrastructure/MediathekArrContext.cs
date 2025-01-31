using MediathekArr.Models.Tvdb;
using Microsoft.EntityFrameworkCore;

namespace MediathekArr.Infrastructure;

public class MediathekArrContext : DbContext
{
    public MediathekArrContext(DbContextOptions<MediathekArrContext> options) : base(options) { }

    public DbSet<Series> Series { get; set; }
    public DbSet<Episode> Episodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Series>().ToTable("series_cache");
        modelBuilder.Entity<Episode>().ToTable("episodes");
    }
}