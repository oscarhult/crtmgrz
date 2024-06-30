using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace crtmgrz;

public class CertificatesContext : DbContext
{
    public DbSet<Certificate> Certificates { get; set; }

    protected CertificatesContext() { }

    public CertificatesContext(DbContextOptions<CertificatesContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Certificate>(entity =>
        {
            entity.HasKey(x => x.Id);
        });
    }
}

public class Certificate
{
    public required Guid Id { get; init; }
    public Guid? Pid { get; init; }

    public required bool Authoritative { get; init; }
    public required string Name { get; init; }
    public required string PrivateKeyPem { get; init; }
    public required string CertificatePem { get; init; }
    public required string NotBefore { get; init; }
    public required string NotAfter { get; init; }

    [ForeignKey(nameof(Pid))]
    public Certificate Parent { get; init; }

    public List<Certificate> Children { get; set; }
}
