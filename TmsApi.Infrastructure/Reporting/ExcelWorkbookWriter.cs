using System.IO.Compression;
using System.Security;
using System.Text;
using TmsApi.Application.Reports;

namespace TmsApi.Infrastructure.Reporting;

/// <summary>Writes a minimal standards-compliant XLSX workbook without another NuGet dependency.</summary>
internal static class ExcelWorkbookWriter
{
    public static byte[] Write(ReportPage report)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Add(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Add(zip, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Add(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            var keys = report.Items.FirstOrDefault()?.Keys.ToArray() ?? ColumnsFor(report.ReportType);
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            xml.Append("<row r=\"1\">");
            for (var col = 0; col < keys.Length; col++) Cell(xml, keys[col], ColumnName(col + 1) + "1");
            xml.Append("</row>");
            var rowNo = 2;
            foreach (var item in report.Items)
            {
                xml.Append($"<row r=\"{rowNo}\">");
                for (var col = 0; col < keys.Length; col++)
                {
                    var key = keys[col];
                    Cell(xml, item.TryGetValue(key, out var value) ? value : null, ColumnName(col + 1) + rowNo);
                }
                xml.Append("</row>"); rowNo++;
            }
            xml.Append("</sheetData></worksheet>");
            Add(zip, "xl/worksheets/sheet1.xml", xml.ToString());
        }
        return output.ToArray();
    }

    private static string[] ColumnsFor(string type) => type switch
    {
        "students" => new[] { "studentId", "registrationNumber", "name", "gpa", "status" },
        "enrollment" => new[] { "enrollmentId", "student", "registrationNumber", "course", "status", "enrolledAt", "year" },
        "courses" => new[] { "courseId", "code", "course", "instructorId", "capacity", "approvedEnrollments", "utilizationPercent" },
        "attendance" => new[] { "courseId", "course", "present", "late", "absent", "excused", "attendancePercent" },
        "performance" => new[] { "student", "registrationNumber", "course", "assessmentId", "score", "maxScore", "percentage", "result", "publishedAt" },
        "certificates" => new[] { "serialNumber", "student", "course", "status", "issuedAt", "revokedAt", "revocationReason" },
        "instructors" => new[] { "instructorId", "instructor", "email", "assignedCourses", "sessions", "courses" },
        "completion" => new[] { "student", "registrationNumber", "course", "certificateNumber", "completedAt" },
        _ => Array.Empty<string>()
    };

    private static void Cell(StringBuilder xml, object? value, string address)
    {
        if (value is null) { xml.Append($"<c r=\"{address}\" t=\"inlineStr\"><is><t></t></is></c>"); return; }
        if (value is bool boolean) { xml.Append($"<c r=\"{address}\" t=\"b\"><v>{(boolean ? 1 : 0)}</v></c>"); return; }
        if (value is byte or short or int or long or float or double or decimal)
        {
            var number = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            xml.Append($"<c r=\"{address}\" t=\"n\"><v>{number}</v></c>"); return;
        }
        var text = value is DateTime date ? date.ToString("O", System.Globalization.CultureInfo.InvariantCulture) : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        xml.Append($"<c r=\"{address}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{SecurityElement.Escape(text)}</t></is></c>");
    }
    private static string ColumnName(int n) { var s=""; while(n>0){n--;s=(char)('A'+n%26)+s;n/=26;} return s; }
    private static void Add(ZipArchive zip, string path, string content)
    { var entry=zip.CreateEntry(path, CompressionLevel.Optimal); using var writer=new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(content); }
}
