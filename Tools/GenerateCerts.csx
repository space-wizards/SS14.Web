#!/usr/bin/dotnet run
#:sdk Microsoft.NET.Sdk.Web

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using var rsaAlgorithm = RSA.Create(keySizeInBits: 2048);

var subject = new X500DistinguishedName("CN=SpaceWizards");

// RSA Encryption Certificate
var request = new CertificateRequest(subject, rsaAlgorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true));

var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));

File.WriteAllBytes("server-encryption-certificate.pfx", certificate.Export(X509ContentType.Pfx, string.Empty));

// RSA Signing Certificate
request = new CertificateRequest(subject, rsaAlgorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));

File.WriteAllBytes("server-signing-certificate-rsa.pfx", certificate.Export(X509ContentType.Pfx, string.Empty));

// RSA-PSS Signing Certificate
request = new CertificateRequest(subject, rsaAlgorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));

File.WriteAllBytes("server-signing-certificate-rsa-pss.pfx", certificate.Export(X509ContentType.Pfx, string.Empty));
