using Microsoft.EntityFrameworkCore;
using Newsletter.Api.Entities;
using System.Text.Json;

namespace Newsletter.Api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>().Property(p => p.Tags).HasConversion(
            to => JsonSerializer.Serialize(to, (JsonSerializerOptions?)null),
            from => JsonSerializer.Deserialize<List<string>>(from, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new StringListValueComparer());
    }

    public DbSet<Article> Articles { get; set; }
}
