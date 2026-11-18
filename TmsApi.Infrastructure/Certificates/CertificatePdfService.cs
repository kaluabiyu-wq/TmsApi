using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using TmsApi.Application.Certificates;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Certificates;

public sealed class CertificatePdfService : ICertificatePdfService
{
    public Task<byte[]> GenerateAsync(
        Certificate certificate, CertificateTemplate template, string verificationUrl,
        CancellationToken ct)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
        var qrBytes = new PngByteQRCode(qrData).GetGraphic(8);

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(template.LogoPath) && File.Exists(template.LogoPath))
            logoBytes = File.ReadAllBytes(template.LogoPath);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(12));
                page.Content().Border(5).BorderColor(Colors.Grey.Darken2).Padding(25).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().AlignCenter().Text(template.InstitutionName)
                        .FontSize(24).Bold().FontColor(Colors.Blue.Darken3);

                    if (logoBytes is not null)
                        column.Item().AlignCenter().Height(65).Image(logoBytes).FitArea();

                    column.Item().PaddingTop(12).AlignCenter().Text(template.CertificateTitle)
                        .FontSize(30).Bold().FontColor(Colors.Grey.Darken3);

                    column.Item().AlignCenter().Text(template.BodyText).FontSize(15);
                    column.Item().AlignCenter().Text(certificate.Student.Name)
                        .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                    column.Item().AlignCenter().Text("has successfully completed")
                        .FontSize(15);
                    column.Item().AlignCenter().Text(certificate.Course.Title)
                        .FontSize(23).Bold();

                    column.Item().PaddingTop(8).AlignCenter()
                        .Text($"Completion date: {certificate.IssuedAt:dd MMMM yyyy}");

                    column.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Certificate No: {certificate.SerialNumber}").Bold();
                            left.Item().Text($"Issued: {certificate.IssuedAt:dd MMMM yyyy}");
                            left.Item().PaddingTop(18).Text(template.SignatureName ?? "Authorized Signatory").Bold();
                            left.Item().Text(template.SignatureTitle ?? "Authorized Signature");
                        });
                        row.ConstantItem(110).Column(right =>
                        {
                            right.Item().AlignCenter().Image(qrBytes).FitArea();
                            right.Item().AlignCenter().Text("Scan to verify").FontSize(9);
                        });
                    });
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
