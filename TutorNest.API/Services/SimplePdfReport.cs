using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TutorNest.API.Services
{
    public static class SimplePdfReport
    {
        public static byte[] Generate(string title, string subtitle, string[] headers, List<string[]> rows)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(false)); // Write without BOM for PDF compliance

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

            int rowsPerPage = 22;
            int pageCount = (int)Math.Ceiling((double)rows.Count / rowsPerPage);
            if (pageCount == 0) pageCount = 1;

            var kidsStr = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
            {
                kidsStr.Append($"{5 + i * 2} 0 R ");
            }

            // 2. Pages Parent
            startObj(2);
            writer.Write($"<< /Type /Pages /Kids [ {kidsStr} ] /Count {pageCount} >>\n");
            endObj();

            // 3. Regular Font
            startObj(3);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
            endObj();

            // 4. Bold Font
            startObj(4);
            writer.Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\n");
            endObj();

            for (int p = 0; p < pageCount; p++)
            {
                int pageObjId = 5 + p * 2;
                int contentObjId = 6 + p * 2;

                // Page Object
                startObj(pageObjId);
                writer.Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjId} 0 R >>\n");
                endObj();

                // Page Content
                var cb = new StringBuilder();
                
                // Document Header (Title)
                cb.Append("BT\n/F2 16 Tf\n50 780 Td\n");
                cb.Append($"({EscapePdfString(title)}) Tj\nET\n");

                // Subtitle
                cb.Append("BT\n/F1 10 Tf\n50 760 Td\n");
                cb.Append($"({EscapePdfString(subtitle)}) Tj\nET\n");

                // Decorative Line separator
                cb.Append("1 w\n0.1 0.6 0.8 RG\n50 745 m\n545 745 l\nS\n"); // Teal line

                // Draw Table Columns Header
                int startY = 720;
                int xOffset = 50;
                int colWidth = 495 / headers.Length;

                cb.Append("BT\n/F2 9 Tf\n");
                for (int i = 0; i < headers.Length; i++)
                {
                    int colX = xOffset + (i * colWidth);
                    if (i == 0)
                    {
                        cb.Append($"{colX} {startY} Td ({EscapePdfString(headers[i])}) Tj\n");
                    }
                    else
                    {
                        int prevColX = xOffset + ((i - 1) * colWidth);
                        cb.Append($"{colX - prevColX} 0 Td ({EscapePdfString(headers[i])}) Tj\n");
                    }
                }
                cb.Append("ET\n");

                // Line below headers
                cb.Append($"0.5 w\n0.6 0.6 0.6 RG\n50 {startY - 5} m\n545 {startY - 5} l\nS\n");

                // Populate Rows
                int pageStart = p * rowsPerPage;
                int pageEnd = Math.Min(pageStart + rowsPerPage, rows.Count);
                int currentY = startY - 20;

                for (int r = pageStart; r < pageEnd; r++)
                {
                    var rowData = rows[r];
                    cb.Append("BT\n/F1 8 Tf\n");
                    for (int i = 0; i < headers.Length; i++)
                    {
                        int colX = xOffset + (i * colWidth);
                        string text = i < rowData.Length ? rowData[i] : "";
                        if (i == 0)
                        {
                            cb.Append($"{colX} {currentY} Td ({EscapePdfString(text)}) Tj\n");
                        }
                        else
                        {
                            int prevColX = xOffset + ((i - 1) * colWidth);
                            cb.Append($"{colX - prevColX} 0 Td ({EscapePdfString(text)}) Tj\n");
                        }
                    }
                    cb.Append("ET\n");

                    // Row border separator
                    cb.Append($"0.2 w\n0.88 0.88 0.88 RG\n50 {currentY - 4} m\n545 {currentY - 4} l\nS\n");
                    currentY -= 18;
                }

                // Footer Page Numbering
                cb.Append("BT\n/F1 8 Tf\n");
                cb.Append($"260 40 Td (Page {p + 1} of {pageCount}) Tj\nET\n");

                byte[] contentBytes = Encoding.UTF8.GetBytes(cb.ToString());
                
                // Content Object write
                startObj(contentObjId);
                writer.Write($"<< /Length {contentBytes.Length} >>\nstream\n");
                writer.Flush();
                ms.Write(contentBytes, 0, contentBytes.Length);
                writer.Write("\nendstream\n");
                endObj();
            }

            // Cross-reference table position
            writer.Flush();
            long xrefPos = ms.Position;
            writer.Write("xref\n");
            writer.Write($"0 {offsets.Count + 1}\n");
            writer.Write("0000000000 65535 f \n");
            for (int i = 0; i < offsets.Count; i++)
            {
                writer.Write($"{offsets[i]:D10} 00000 n \n");
            }

            // Document trailer
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
