﻿using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace crtmgrz;

public record CertificateResponse(Guid Id, Guid? Pid, string Name, bool Authoritative, string NotBefore, string NotAfter);

public enum ExportFormat { None, Pfx, Cer, Pem, Chain, PrivateKey }

public class CertificateModel
{
    public bool Authoritative { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Years { get; set; } = 1;
    public string Domains { get; set; } = string.Empty;
    public string IPs { get; set; } = string.Empty;
}

public class CertificatesService
{
    private readonly IDbContextFactory<CertificatesContext> fac;

    public CertificatesService(IDbContextFactory<CertificatesContext> fac)
    {
        this.fac = fac;
    }

    public async Task<List<CertificateResponse>> LoadCertificates(Guid? id)
    {
        using var db = await fac.CreateDbContextAsync();

        return await db.Certificates
            .Where(x => x.Pid == id)
            .OrderByDescending(x => x.Authoritative)
            .ThenBy(x => x.Name.ToLower())
            .Select(x => new CertificateResponse
            (
                x.Id,
                x.Pid,
                x.Name,
                x.Authoritative,
                x.NotBefore,
                x.NotAfter
            ))
            .ToListAsync();
    }

    public async Task DownloadCertificate(IJSRuntime js, Guid id, ExportFormat format)
    {
        using var db = await fac.CreateDbContextAsync();

        var certificate = await db.Certificates.SingleAsync(x => x.Id == id);

        if (format == ExportFormat.Pfx)
        {
            using var cert = X509Certificate2.CreateFromPem(certificate.CertificatePem);
            using var key = RSA.Create();
            key.ImportFromPem(certificate.PrivateKeyPem);
            using var certWithKey = cert.CopyWithPrivateKey(key);
            using var stream = new MemoryStream(certWithKey.Export(X509ContentType.Pfx));
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("downloadFileFromStream", $"{certificate.Name}.pfx", streamRef);
        }
        else if (format == ExportFormat.Cer)
        {
            using var cert = X509Certificate2.CreateFromPem(certificate.CertificatePem);
            using var stream = new MemoryStream(cert.Export(X509ContentType.Cert));
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("downloadFileFromStream", $"{certificate.Name}.cer", streamRef);
        }
        else if (format == ExportFormat.Pem)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(certificate.CertificatePem));
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("downloadFileFromStream", $"{certificate.Name}.pem", streamRef);
        }
        else if (format == ExportFormat.Chain)
        {
            var chain = new List<Certificate> { certificate };
            var pid = certificate.Pid;

            while (pid.HasValue)
            {
                var parent = await db.Certificates.SingleOrDefaultAsync(x => x.Id == pid.Value);
                pid = parent?.Pid;
                if (parent != null)
                {
                    chain.Add(parent);
                }
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join('\n', chain.Select(x => x.CertificatePem))));
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("downloadFileFromStream", $"{certificate.Name}.chain", streamRef);
        }
        else if (format == ExportFormat.PrivateKey)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(certificate.PrivateKeyPem));
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("downloadFileFromStream", $"{certificate.Name}.key", streamRef);
        }
    }

    public async Task<Guid> CreateCertificate(CertificateModel model, Guid? pid)
    {
        using var db = await fac.CreateDbContextAsync();

        using var rsa = RSA.Create(model.Authoritative ? 4096 : 2048);

        var now = DateTimeOffset.UtcNow;

        var notBefore = now
            .AddDays(-(now.Day - 1))
            .AddHours(-now.Hour)
            .AddMinutes(-now.Minute)
            .AddSeconds(-now.Second);

        var notAfter = notBefore.AddYears(model.Years).AddSeconds(-1);

        var nameBuilder = new X500DistinguishedNameBuilder();
        nameBuilder.AddOrganizationName(model.Name);
        nameBuilder.AddCommonName(model.Name);
        var name = nameBuilder.Build();

        var certificateRequest = new CertificateRequest(
            name,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        certificateRequest.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") },
                false
            )
        );

        certificateRequest.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false)
        );

        X509Certificate2 cert;

        if (model.Authoritative)
        {
            certificateRequest.CertificateExtensions.Add(
                X509BasicConstraintsExtension.CreateForCertificateAuthority()
            );

            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false)
            );
        }
        else
        {
            certificateRequest.CertificateExtensions.Add(
                X509BasicConstraintsExtension.CreateForEndEntity()
            );

            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false)
            );
        }

        if (pid == null)
        {
            if (!model.Authoritative)
            {
                var subjectBuilder = new SubjectAlternativeNameBuilder();

                if (!string.IsNullOrEmpty(model.Domains))
                {
                    foreach (var domain in model.Domains.Split('\n'))
                    {
                        subjectBuilder.AddDnsName(domain);
                    }
                }

                if (!string.IsNullOrEmpty(model.IPs))
                {
                    foreach (var ip in model.IPs.Split('\n'))
                    {
                        subjectBuilder.AddIpAddress(IPAddress.Parse(ip));
                    }
                }

                certificateRequest.CertificateExtensions.Add(subjectBuilder.Build());
            }

            cert = certificateRequest.CreateSelfSigned(notBefore, notAfter);
        }
        else
        {
            var parent = await db.Certificates.SingleAsync(x => x.Id == pid);

            using var caCert = X509Certificate2.CreateFromPem(parent.CertificatePem);
            using var caKey = RSA.Create();
            caKey.ImportFromPem(parent.PrivateKeyPem);
            using var caCertWithKey = caCert.CopyWithPrivateKey(caKey);

            certificateRequest.CertificateExtensions.Add(
                X509AuthorityKeyIdentifierExtension.CreateFromCertificate(caCertWithKey, true, true)
            );

            if (!model.Authoritative)
            {
                var subjectBuilder = new SubjectAlternativeNameBuilder();

                if (!string.IsNullOrEmpty(model.Domains))
                {
                    foreach (var domain in model.Domains.Split('\n'))
                    {
                        subjectBuilder.AddDnsName(domain);
                    }
                }

                if (!string.IsNullOrEmpty(model.IPs))
                {
                    foreach (var ip in model.IPs.Split('\n'))
                    {
                        subjectBuilder.AddIpAddress(IPAddress.Parse(ip));
                    }
                }

                certificateRequest.CertificateExtensions.Add(subjectBuilder.Build());
            }

            cert = certificateRequest.Create(
                caCertWithKey,
                notBefore,
                notAfter,
                RandomNumberGenerator.GetBytes(8)
            );
        }

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Pid = pid,
            Authoritative = model.Authoritative,
            Name = model.Name,
            PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
            CertificatePem = cert.ExportCertificatePem(),
            NotBefore = notBefore.ToString("yyyy-MM-dd"),
            NotAfter = notAfter.ToString("yyyy-MM-dd"),
        };

        cert.Dispose();

        await db.Certificates.AddAsync(certificate);

        await db.SaveChangesAsync();

        return certificate.Id;
    }

    public async Task DeleteCertificate(Guid id)
    {
        using var db = await fac.CreateDbContextAsync();

        var certificate = await db.Certificates.SingleAsync(x => x.Id == id);

        db.Certificates.Remove(certificate);

        await foreach (var child in GetCertificateChildren(db, certificate))
        {
            db.Certificates.Remove(child);
        }

        await db.SaveChangesAsync();
    }

    public async IAsyncEnumerable<Certificate> GetCertificateChildren(CertificatesContext db, Certificate certificate)
    {
        var children = await db.Certificates.Where(c => c.Pid == certificate.Id).ToListAsync();

        foreach (var child in children)
        {
            yield return child;

            await foreach (var descendant in GetCertificateChildren(db, child))
            {
                yield return descendant;
            }
        }
    }
}