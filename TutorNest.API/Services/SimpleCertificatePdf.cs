using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TutorNest.API.Services
{
    public static class SimpleCertificatePdf
    {
        public static byte[] Generate(
            string studentName,
            string curriculumTitle,
            string teacherName,
            string issuedDate,
            string certCode,
            string? customTitle,
            string? customSubTitle,
            string? customMessage)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(false));

            var offsets = new List<long>();
            
            // Header
            writer.Write("%PDF-1.4\n");
            writer.Flush();
            
            Action<int> startObj = (id) => {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.Write($"{id} 0 obj\n");
            };

            Action endObj = () => {
                writer.Write("endobj\n");
            };

            // 1. Catalog
            startObj(1);
            writer.Write("<< /Type /Catalog /Pages 2 0 R >>\n");
            endObj();

            // 2. Pages Parent
            startObj(2);
            writer.Write("<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>\n");
            endObj();

            // 4. Regular Font
            startObj(4);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
            endObj();

            // 500. Bold Font
            startObj(500);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\n");
            endObj();

            // 3. Page Object (Landscape MediaBox [0 0 842 595])
            startObj(3);
            writer.Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 500 0 R >> >> /Contents 5 0 R >>\n");
            endObj();

            // Generate content stream
            var cb = new StringBuilder();

            // Draw thick outer gold border
            cb.Append("3 w\n0.85 0.68 0.15 RG\n30 30 782 535 re\nS\n");

            // Draw thin inner gold border
            cb.Append("1 w\n0.85 0.68 0.15 RG\n38 38 766 519 re\nS\n");

            // Draw some decorative corner elements (lines)
            // Top-left corner design
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n45 520 m\n45 540 l\n45 540 m\n65 540 l\nS\n");
            // Top-right corner design
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n797 520 m\n797 540 l\n797 540 m\n777 540 l\nS\n");
            // Bottom-left corner design
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n45 75 m\n45 55 l\n45 55 m\n65 55 l\nS\n");
            // Bottom-right corner design
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n797 75 m\n797 55 l\n797 55 m\n777 55 l\nS\n");

            // Center Helper
            Action<string, int, int, string, string> drawCenteredText = (text, fontSize, y, fontName, color) => {
                double charWidth = fontName == "/F2" ? 0.6 : 0.52;
                double width = text.Length * charWidth * fontSize;
                double x = 421 - (width / 2);
                cb.Append($"BT\n{fontName} {fontSize} Tf\n{color}\n{x:F1} {y} Td\n({EscapePdfString(text)}) Tj\nET\n");
            };

            // 1. Academy Branding Header
            drawCenteredText("TUTORNEST ACADEMY", 13, 490, "/F2", "0.45 0.55 0.72 rg"); // Slate blue header

            // 2. Certificate Title (Custom or default)
            string title = !string.IsNullOrEmpty(customTitle) ? customTitle : "CERTIFICATE OF COMPLETION";
            drawCenteredText(title, 26, 430, "/F2", "0.85 0.68 0.15 rg"); // Gold title

            // 3. Subtitle / Presenter text
            string sub = !string.IsNullOrEmpty(customSubTitle) ? customSubTitle : "This certificate is proudly presented to";
            drawCenteredText(sub, 12, 365, "/F1", "0.3 0.3 0.3 rg");

            // 4. Student Name (Larger & Bold)
            drawCenteredText(studentName, 32, 300, "/F2", "0.1 0.1 0.1 rg");

            // 5. Message / Details text
            string msg = !string.IsNullOrEmpty(customMessage) ? customMessage : "for successfully completing the curriculum requirements for";
            drawCenteredText(msg, 12, 240, "/F1", "0.3 0.3 0.3 rg");

            // 6. Course/Class Title
            drawCenteredText(curriculumTitle, 18, 195, "/F2", "0.85 0.68 0.15 rg");

            // Decorative separator below curriculum
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n321 170 m\n521 170 l\nS\n");

            // 7. Footer lines & signers
            // Left Line (Date)
            cb.Append("0.5 w\n0.6 0.6 0.6 RG\n90 120 m\n290 120 l\nS\n");
            cb.Append($"BT\n/F1 10 Tf\n0.3 0.3 0.3 rg\n90 102 Td\n(Date Issued: {EscapePdfString(issuedDate)}) Tj\nET\n");

            // Right Line (Tutor)
            cb.Append("0.5 w\n0.6 0.6 0.6 RG\n552 120 m\n752 120 l\nS\n");
            cb.Append($"BT\n/F2 10 Tf\n0.1 0.1 0.1 rg\n552 102 Td\n(Authorized Tutor: {EscapePdfString(teacherName)}) Tj\nET\n");

            // 8. Verification Code
            string codeText = $"Verification ID Code: {certCode}";
            drawCenteredText(codeText, 8, 65, "/F1", "0.5 0.5 0.5 rg");

            byte[] contentBytes = Encoding.UTF8.GetBytes(cb.ToString());

            // 5. Content Object
            startObj(5);
            writer.Write($"<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Flush();
            ms.Write(contentBytes, 0, contentBytes.Length);
            writer.Write("\nendstream\n");
            endObj();

            // Cross-reference table
            writer.Flush();
            long xrefPos = ms.Position;
            writer.Write("xref\n");
            writer.Write($"0 {offsets.Count + 1}\n");
            writer.Write("0000000000 65535 f \n");
            for (int i = 0; i < offsets.Count; i++)
            {
                writer.Write($"{offsets[i]:D10} 00000 n \n");
            }

            // Trailer
            writer.Write($"trailer << /Size {offsets.Count + 1} /Root 1 0 R >>\n");
            writer.Write("startxref\n");
            writer.Write($"{xrefPos}\n");
            writer.Write("%%EOF\n");
            writer.Flush();

            return ms.ToArray();
        }

        private static string EscapePdfString(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (c == '(' || c == ')' || c == '\\')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
