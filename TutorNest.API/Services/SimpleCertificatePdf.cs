using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;

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
            string? customMessage,
            string? logoUrl)
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

            // Download and parse image logo if provided
            byte[]? imageBytes = null;
            int imgWidth = 0;
            int imgHeight = 0;
            bool isPng = false;

            if (!string.IsNullOrEmpty(logoUrl))
            {
                var imgInfo = GetImageInfo(logoUrl);
                imageBytes = imgInfo.bytes;
                imgWidth = imgInfo.width;
                imgHeight = imgInfo.height;
                isPng = imgInfo.isPng;
            }

            bool hasLogo = imageBytes != null && imgWidth > 0 && imgHeight > 0;

            // 1. Catalog
            startObj(1);
            writer.Write("<< /Type /Catalog /Pages 2 0 R >>\n");
            endObj();

            // 2. Pages Parent
            startObj(2);
            writer.Write("<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>\n");
            endObj();

            // 3. Page Object (Landscape MediaBox [0 0 842 595])
            startObj(3);
            if (hasLogo)
            {
                writer.Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> /XObject << /Logo 7 0 R >> >> /Contents 6 0 R >>\n");
            }
            else
            {
                writer.Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>\n");
            }
            endObj();

            // 4. Regular Font
            startObj(4);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
            endObj();

            // 5. Bold Font
            startObj(5);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\n");
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

            // Draw Tuition Logo if available
            if (hasLogo)
            {
                double logoMaxDim = 60.0;
                double logoW = imgWidth;
                double logoH = imgHeight;
                if (logoW > logoH)
                {
                    logoH = (logoH / logoW) * logoMaxDim;
                    logoW = logoMaxDim;
                }
                else
                {
                    logoW = (logoW / logoH) * logoMaxDim;
                    logoH = logoMaxDim;
                }
                double logoX = 60;
                double logoY = 510 - logoH; // Placed inside top-left area
                cb.Append($"q\n{logoW:F2} 0 0 {logoH:F2} {logoX:F2} {logoY:F2} cm\n/Logo Do\nQ\n");
            }

            // Draw Gold/Red Badge at Bottom Center
            // 1. Red ribbons first
            cb.Append("0.75 0.15 0.15 rg\n");
            cb.Append("406 110 m 396 70 l 408 78 l 416 110 l f\n");
            cb.Append("436 110 m 446 70 l 434 78 l 426 110 l f\n");

            // 2. Gold outer seal star (overlapping squares)
            cb.Append("0.85 0.68 0.15 rg 0.65 0.52 0.1 RG 1 w\n");
            cb.Append("401 100 m 441 100 l 441 140 l 401 140 l b\n");
            cb.Append("421 92 m 449 120 l 421 148 l 393 120 l b\n");

            // 3. Red center seal circle/octagon
            cb.Append("0.75 0.15 0.15 rg 0.55 0.1 0.1 RG 1 w\n");
            cb.Append("413 108 m 429 108 l 433 112 l 433 128 l 429 132 l 413 132 l 409 128 l 409 112 l b\n");

            // 4. Print 'TN' (TutorNest) in center
            cb.Append("BT\n/F2 7 Tf\n0.85 0.68 0.15 rg\n415 117 Td\n(TN) Tj\nET\n");

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

            // 6. Content Object
            startObj(6);
            writer.Write($"<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Flush();
            ms.Write(contentBytes, 0, contentBytes.Length);
            writer.Write("\nendstream\n");
            endObj();

            // 7. Image XObject (if any)
            if (hasLogo)
            {
                startObj(7);
                if (isPng)
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {imgWidth} /Height {imgHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /DecodeParms << /Predictor 15 /Colors 3 /BitsPerComponent 8 /Columns {imgWidth} >> /Length {imageBytes!.Length} >>\nstream\n");
                }
                else
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {imgWidth} /Height {imgHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {imageBytes!.Length} >>\nstream\n");
                }
                writer.Flush();
                ms.Write(imageBytes, 0, imageBytes.Length);
                writer.Write("\nendstream\n");
                endObj();
            }

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

        private static (byte[]? bytes, int width, int height, bool isPng) GetImageInfo(string logoUrl)
        {
            try
            {
                using var client = new HttpClient();
                byte[] bytes = client.GetByteArrayAsync(logoUrl).GetAwaiter().GetResult();
                
                if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) // PNG signature
                {
                    // PNG dimensions at bytes 16-19 (Width) and 20-23 (Height)
                    int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    
                    // Extract IDAT chunks
                    using var ms = new MemoryStream();
                    int idx = 8;
                    while (idx < bytes.Length - 12)
                    {
                        int length = (bytes[idx] << 24) | (bytes[idx + 1] << 16) | (bytes[idx + 2] << 8) | bytes[idx + 3];
                        string chunkType = Encoding.ASCII.GetString(bytes, idx + 4, 4);
                        if (chunkType == "IDAT")
                        {
                            ms.Write(bytes, idx + 8, length);
                        }
                        idx += 12 + length;
                    }
                    return (ms.ToArray(), width, height, true);
                }
                else // Assume JPEG
                {
                    int width = 100;
                    int height = 100;
                    for (int i = 0; i < bytes.Length - 8; i++)
                    {
                        if (bytes[i] == 0xFF && (bytes[i + 1] == 0xC0 || bytes[i + 1] == 0xC2)) // SOF0 or SOF2
                        {
                            height = (bytes[i + 5] << 8) | bytes[i + 6];
                            width = (bytes[i + 7] << 8) | bytes[i + 8];
                            break;
                        }
                    }
                    return (bytes, width, height, false);
                }
            }
            catch
            {
                return (null, 0, 0, false);
            }
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
