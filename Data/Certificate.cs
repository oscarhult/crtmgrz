using System.ComponentModel.DataAnnotations.Schema;

namespace crtmgrz.Data;

public class Certificate
{
    public required Guid Id { get; init; }
    public Guid? Pid { get; init; }

    public required bool Authoritative { get; init; }
    public required string Name { get; init; }
    public required string PrivateKeyPem { get; init; }
    public required string CertificatePem { get; init; }

    [ForeignKey(nameof(Pid))]
    public Certificate Parent { get; init; }

    public List<Certificate> Children { get; set; }
}