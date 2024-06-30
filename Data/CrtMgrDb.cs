using Microsoft.EntityFrameworkCore;

namespace crtmgrz.Data;

public class CrtMgrDb : DbContext
{
    public DbSet<Certificate> Certificates { get; set; }

    protected CrtMgrDb() { }

    public CrtMgrDb(DbContextOptions<CrtMgrDb> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Certificate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }
}