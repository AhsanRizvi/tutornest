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

            // 1. Download and parse Tuition Logo if provided
            byte[]? logoBytes = null;
            byte[]? logoAlpha = null;
            int logoW = 0;
            int logoH = 0;
            bool logoIsPng = false;

            if (!string.IsNullOrEmpty(logoUrl))
            {
                var imgInfo = GetImageInfo(logoUrl);
                logoBytes = imgInfo.bytes;
                logoAlpha = imgInfo.alpha;
                logoW = imgInfo.width;
                logoH = imgInfo.height;
                logoIsPng = imgInfo.isPng;
            }
            bool hasLogo = logoBytes != null && logoW > 0 && logoH > 0;

            // 2. Load and parse Local Official Red/Gold Seal PNG
            var sealInfo = LoadLocalSeal();
            byte[]? sealBytes = sealInfo.bytes;
            byte[]? sealAlpha = sealInfo.alpha;
            int sealW = sealInfo.width;
            int sealH = sealInfo.height;
            bool hasSeal = sealBytes != null && sealW > 0 && sealH > 0;

            // 3. Download QR Code JPEG for Verification
            byte[]? qrBytes = GetQrCodeBytes(certCode);
            int qrW = 150;
            int qrH = 150;
            if (qrBytes != null)
            {
                for (int i = 0; i < qrBytes.Length - 8; i++)
                {
                    if (qrBytes[i] == 0xFF && (qrBytes[i + 1] == 0xC0 || qrBytes[i + 1] == 0xC2))
                    {
                        qrH = (qrBytes[i + 5] << 8) | qrBytes[i + 6];
                        qrW = (qrBytes[i + 7] << 8) | qrBytes[i + 8];
                        break;
                    }
                }
            }
            bool hasQr = qrBytes != null;

            // 4. Catalog
            startObj(1);
            writer.Write("<< /Type /Catalog /Pages 2 0 R >>\n");
            endObj();

            // 5. Pages Parent
            startObj(2);
            writer.Write("<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>\n");
            endObj();

            // Dynamic Object ID Allocation from 7 onwards
            int nextId = 7;
            var xobjectDict = new StringBuilder();

            int logoImgId = 0;
            int logoMaskId = 0;
            if (hasLogo)
            {
                logoImgId = nextId++;
                xobjectDict.Append($"/Logo {logoImgId} 0 R ");
                if (logoIsPng && logoAlpha != null)
                {
                    logoMaskId = nextId++;
                }
            }

            int sealImgId = 0;
            int sealMaskId = 0;
            if (hasSeal)
            {
                sealImgId = nextId++;
                xobjectDict.Append($"/Seal {sealImgId} 0 R ");
                if (sealAlpha != null)
                {
                    sealMaskId = nextId++;
                }
            }

            int qrImgId = 0;
            if (hasQr)
            {
                qrImgId = nextId++;
                xobjectDict.Append($"/QR {qrImgId} 0 R ");
            }

            // 6. Page Object (Landscape MediaBox [0 0 842 595])
            startObj(3);
            if (xobjectDict.Length > 0)
            {
                writer.Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> /XObject << {xobjectDict} >> >> /Contents 6 0 R >>\n");
            }
            else
            {
                writer.Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>\n");
            }
            endObj();

            // 7. Regular Font
            startObj(4);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
            endObj();

            // 8. Bold Font
            startObj(5);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\n");
            endObj();

            // Generate content stream (Page content)
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

            // Draw Tuition Logo Centered at Top
            if (hasLogo)
            {
                double logoMaxDim = 60.0;
                double logoWActual = logoW;
                double logoHActual = logoH;
                if (logoWActual > logoHActual)
                {
                    logoHActual = (logoHActual / logoWActual) * logoMaxDim;
                    logoWActual = logoMaxDim;
                }
                else
                {
                    logoWActual = (logoWActual / logoHActual) * logoMaxDim;
                    logoHActual = logoMaxDim;
                }
                double logoX = 421 - (logoWActual / 2);
                double logoY = 530 - logoHActual; // Top aligned below the border
                cb.Append($"q\n{logoWActual:F2} 0 0 {logoHActual:F2} {logoX:F2} {logoY:F2} cm\n/Logo Do\nQ\n");
            }

            // Draw Official Red/Gold Seal Centered at Bottom
            if (hasSeal)
            {
                double sealSize = 100.0;
                double sealX = 421 - (sealSize / 2);
                double sealY = 70; // Positioned between Date and Signature lines
                cb.Append($"q\n{sealSize:F2} 0 0 {sealSize:F2} {sealX:F2} {sealY:F2} cm\n/Seal Do\nQ\n");
            }

            // Draw Verification QR Code at Bottom Right
            if (hasQr)
            {
                double qrSize = 50.0;
                double qrX = 730;
                double qrY = 50; // Inside thin border corner
                cb.Append($"q\n{qrSize:F2} 0 0 {qrSize:F2} {qrX:F2} {qrY:F2} cm\n/QR Do\nQ\n");
            }

            // Center Helper
            Action<string, int, int, string, string> drawCenteredText = (text, fontSize, y, fontName, color) => {
                double charWidth = fontName == "/F2" ? 0.6 : 0.52;
                double width = text.Length * charWidth * fontSize;
                double x = 421 - (width / 2);
                cb.Append($"BT\n{fontName} {fontSize} Tf\n{color}\n{x:F1} {y} Td\n({EscapePdfString(text)}) Tj\nET\n");
            };

            // 1. Academy Branding Header
            drawCenteredText("TUTORNEST ACADEMY", 13, 455, "/F2", "0.45 0.55 0.72 rg");

            // 2. Certificate Title (Custom or default)
            string title = !string.IsNullOrEmpty(customTitle) ? customTitle : "CERTIFICATE OF COMPLETION";
            drawCenteredText(title, 26, 405, "/F2", "0.85 0.68 0.15 rg"); // Gold title

            // 3. Subtitle / Presenter text
            string sub = !string.IsNullOrEmpty(customSubTitle) ? customSubTitle : "This certificate is proudly presented to";
            drawCenteredText(sub, 12, 350, "/F1", "0.3 0.3 0.3 rg");

            // 4. Student Name (Larger & Bold)
            drawCenteredText(studentName, 32, 290, "/F2", "0.1 0.1 0.1 rg");

            // 5. Message / Details text
            string msg = !string.IsNullOrEmpty(customMessage) ? customMessage : "for successfully completing the curriculum requirements for";
            drawCenteredText(msg, 12, 235, "/F1", "0.3 0.3 0.3 rg");

            // 6. Course/Class Title
            drawCenteredText(curriculumTitle, 18, 190, "/F2", "0.85 0.68 0.15 rg");

            // Decorative separator below curriculum
            cb.Append("0.5 w\n0.85 0.68 0.15 RG\n321 165 m\n521 165 l\nS\n");

            // 7. Footer lines & signers
            // Left Line (Date)
            cb.Append("0.5 w\n0.6 0.6 0.6 RG\n90 120 m\n290 120 l\nS\n");
            cb.Append($"BT\n/F1 10 Tf\n0.3 0.3 0.3 rg\n90 102 Td\n(Date Issued: {EscapePdfString(issuedDate)}) Tj\nET\n");

            // Right Line (Tutor)
            cb.Append("0.5 w\n0.6 0.6 0.6 RG\n500 120 m\n700 120 l\nS\n");
            cb.Append($"BT\n/F2 10 Tf\n0.1 0.1 0.1 rg\n500 102 Td\n(Authorized Tutor: {EscapePdfString(teacherName)}) Tj\nET\n");

            byte[] contentBytes = Encoding.UTF8.GetBytes(cb.ToString());

            // 6. Content Object
            startObj(6);
            writer.Write($"<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Flush();
            ms.Write(contentBytes, 0, contentBytes.Length);
            writer.Write("\nendstream\n");
            endObj();

            // 7. Write Tuition Logo Object
            if (hasLogo)
            {
                startObj(logoImgId);
                if (logoIsPng && logoAlpha != null)
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {logoW} /Height {logoH} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /SMask {logoMaskId} 0 R /Length {logoBytes!.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(logoBytes, 0, logoBytes.Length);
                    writer.Write("\nendstream\n");
                    endObj();

                    // Write Logo Mask Object
                    startObj(logoMaskId);
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {logoW} /Height {logoH} /ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode /Length {logoAlpha.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(logoAlpha, 0, logoAlpha.Length);
                    writer.Write("\nendstream\n");
                    endObj();
                }
                else
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {logoW} /Height {logoH} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {logoBytes!.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(logoBytes, 0, logoBytes.Length);
                    writer.Write("\nendstream\n");
                    endObj();
                }
            }

            // 8. Write Official Seal Object
            if (hasSeal)
            {
                startObj(sealImgId);
                if (sealAlpha != null)
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {sealW} /Height {sealH} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /SMask {sealMaskId} 0 R /Length {sealBytes!.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(sealBytes, 0, sealBytes.Length);
                    writer.Write("\nendstream\n");
                    endObj();

                    // Write Seal Mask Object
                    startObj(sealMaskId);
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {sealW} /Height {sealH} /ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode /Length {sealAlpha.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(sealAlpha, 0, sealAlpha.Length);
                    writer.Write("\nendstream\n");
                    endObj();
                }
                else
                {
                    writer.Write($"<< /Type /XObject /Subtype /Image /Width {sealW} /Height {sealH} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {sealBytes!.Length} >>\nstream\n");
                    writer.Flush();
                    ms.Write(sealBytes, 0, sealBytes.Length);
                    writer.Write("\nendstream\n");
                    endObj();
                }
            }

            // 9. Write QR Code Object
            if (hasQr)
            {
                startObj(qrImgId);
                writer.Write($"<< /Type /XObject /Subtype /Image /Width {qrW} /Height {qrH} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {qrBytes!.Length} >>\nstream\n");
                writer.Flush();
                ms.Write(qrBytes, 0, qrBytes.Length);
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

        private static (byte[]? bytes, byte[]? alpha, int width, int height, bool isPng) LoadLocalSeal()
        {
            try
            {
                string sealPath = Path.Combine(AppContext.BaseDirectory, "uploads", "red_gold_seal.png");
                if (!File.Exists(sealPath))
                {
                    sealPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "red_gold_seal.png");
                }
                if (!File.Exists(sealPath))
                {
                    sealPath = Path.Combine(Directory.GetCurrentDirectory(), "TutorNest.API", "uploads", "red_gold_seal.png");
                }
                
                if (!File.Exists(sealPath))
                {
                    return (null, null, 0, 0, false);
                }
                
                byte[] bytes = File.ReadAllBytes(sealPath);
                return ParsePngBytes(bytes);
            }
            catch
            {
                return (null, null, 0, 0, false);
            }
        }

        private static (byte[]? bytes, byte[]? alpha, int width, int height, bool isPng) GetImageInfo(string logoUrl)
        {
            try
            {
                using var client = new HttpClient();
                byte[] bytes = client.GetByteArrayAsync(logoUrl).GetAwaiter().GetResult();
                
                if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) // PNG signature
                {
                    return ParsePngBytes(bytes);
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
                    return (bytes, null, width, height, false);
                }
            }
            catch
            {
                return (null, null, 0, 0, false);
            }
        }

        private static (byte[]? bytes, byte[]? alpha, int width, int height, bool isPng) ParsePngBytes(byte[] bytes)
        {
            try
            {
                if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    byte colorType = bytes[25];
                    
                    if (colorType != 2 && colorType != 6) // Only support RGB (2) and RGBA (6)
                    {
                        return (null, null, 0, 0, false);
                    }
                    
                    int bpp = colorType == 6 ? 4 : 3;
                    
                    // Extract IDAT chunks
                    using var idatMs = new MemoryStream();
                    int idx = 8;
                    while (idx < bytes.Length - 12)
                    {
                        int length = (bytes[idx] << 24) | (bytes[idx + 1] << 16) | (bytes[idx + 2] << 8) | bytes[idx + 3];
                        string chunkType = Encoding.ASCII.GetString(bytes, idx + 4, 4);
                        if (chunkType == "IDAT")
                        {
                            idatMs.Write(bytes, idx + 8, length);
                        }
                        idx += 12 + length;
                    }
                    
                    byte[] idatBytes = idatMs.ToArray();
                    byte[] decompressed = DecompressZlib(idatBytes);
                    
                    int stride = width * bpp + 1;
                    byte[] rgbData = new byte[width * height * 3];
                    byte[] alphaData = new byte[width * height];
                    
                    byte[] prevRow = new byte[width * bpp];
                    byte[] currRow = new byte[width * bpp];
                    
                    for (int y = 0; y < height; y++)
                    {
                        int rawOffset = y * stride;
                        byte filterType = decompressed[rawOffset];
                        
                        for (int i = 0; i < width * bpp; i++)
                        {
                            byte rawByte = decompressed[rawOffset + 1 + i];
                            byte a = i >= bpp ? currRow[i - bpp] : (byte)0;
                            byte b = prevRow[i];
                            byte c = i >= bpp ? prevRow[i - bpp] : (byte)0;
                            
                            byte reconVal = filterType switch
                            {
                                0 => rawByte,
                                1 => (byte)((rawByte + a) & 0xFF),
                                2 => (byte)((rawByte + b) & 0xFF),
                                3 => (byte)((rawByte + (a + b) / 2) & 0xFF),
                                4 => (byte)((rawByte + PaethPredictor(a, b, c)) & 0xFF),
                                _ => rawByte
                            };
                            
                            currRow[i] = reconVal;
                        }
                        
                        for (int x = 0; x < width; x++)
                        {
                            int pixelIdx = x * bpp;
                            int rgbIdx = (y * width + x) * 3;
                            rgbData[rgbIdx] = currRow[pixelIdx];
                            rgbData[rgbIdx + 1] = currRow[pixelIdx + 1];
                            rgbData[rgbIdx + 2] = currRow[pixelIdx + 2];
                            
                            if (bpp == 4)
                            {
                                alphaData[y * width + x] = currRow[pixelIdx + 3];
                            }
                            else
                            {
                                alphaData[y * width + x] = 255;
                            }
                        }
                        
                        Array.Copy(currRow, prevRow, currRow.Length);
                    }
                    
                    byte[] compressedRgb = CompressZlib(rgbData);
                    byte[]? compressedAlpha = bpp == 4 ? CompressZlib(alphaData) : null;
                    
                    return (compressedRgb, compressedAlpha, width, height, true);
                }
            }
            catch
            {
                // ignore
            }
            return (null, null, 0, 0, false);
        }

        private static byte[] DecompressZlib(byte[] data)
        {
            // Skip 2 bytes of zlib header (usually 78 9C)
            using var inputMs = new MemoryStream(data, 2, data.Length - 6); // skip 2 bytes header and 4 bytes adler checksum
            using var deflate = new System.IO.Compression.DeflateStream(inputMs, System.IO.Compression.CompressionMode.Decompress);
            using var outputMs = new MemoryStream();
            deflate.CopyTo(outputMs);
            return outputMs.ToArray();
        }

        private static byte[] CompressZlib(byte[] data)
        {
            using var outputMs = new MemoryStream();
            outputMs.WriteByte(0x78);
            outputMs.WriteByte(0x9C);
            using (var deflate = new System.IO.Compression.DeflateStream(outputMs, System.IO.Compression.CompressionLevel.Optimal, true))
            {
                deflate.Write(data, 0, data.Length);
            }
            uint adler = CalculateAdler32(data);
            outputMs.WriteByte((byte)((adler >> 24) & 0xFF));
            outputMs.WriteByte((byte)((adler >> 16) & 0xFF));
            outputMs.WriteByte((byte)((adler >> 8) & 0xFF));
            outputMs.WriteByte((byte)(adler & 0xFF));
            return outputMs.ToArray();
        }

        private static uint CalculateAdler32(byte[] data)
        {
            uint s1 = 1;
            uint s2 = 0;
            foreach (byte b in data)
            {
                s1 = (s1 + b) % 65521;
                s2 = (s2 + s1) % 65521;
            }
            return (s2 << 16) | s1;
        }

        private static byte PaethPredictor(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        private static byte[]? GetQrCodeBytes(string certCode)
        {
            try
            {
                using var client = new HttpClient();
                string url = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&format=jpeg&data={Uri.EscapeDataString(certCode)}";
                return client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
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
