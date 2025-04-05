
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//For Parsing PDF documents
using UglyToad.PdfPig;

//For Parsing HTML documents
using HtmlAgilityPack;

//For Parsing Word Documents
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;

namespace LLama.Rag.RagTools
{

    public static class DocumentParser
    {
        private static readonly Dictionary<string, Func<string, SourceFile>> SupportedFileTypes =
        new Dictionary<string, Func<string, SourceFile>>(StringComparer.OrdinalIgnoreCase)
        {
                    { ".pdf", ParsePDF },
                    { ".docx", ParseWord },
                    { ".txt", ParseText },
                    { ".html", ParseHTML },
                    { ".htm", ParseHTML }
};
       
        public static SourceFile ParsePDF(string path)
        {
            var sb = new StringBuilder();
            
            using (PdfDocument document = PdfDocument.Open(path))
            {
                foreach (var page in document.GetPages())
                {
                    var words = page.GetWords();

                    foreach (var word in words)
                    {
                        sb.Append(word.Text);
                        sb.Append(' '); // Preserve natural spacing between words
                    }

                    sb.Append(" "); // Page delimiter (optional)
                }
            }

            // Normalize spaces: collapse multiple spaces into one
            string cleaned = Regex.Replace(sb.ToString(), @" {2,}", " ").Trim();

            return new SourceFile
            {
                Url = path,
                TextContent = cleaned,
                FileName = Path.GetFileNameWithoutExtension(path),
                FileType = Path.GetExtension(path),
                DateLastSaved = File.GetLastWriteTime(path)
            };
        }

        public static SourceFile ParseWord(string path)
        {
            var sb = new StringBuilder();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;

                if (body != null)
                {
                    foreach (var para in body.Elements<Paragraph>())
                    {
                        sb.AppendLine(para.InnerText);
                    }
                }
            }

            string text = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();

            return new SourceFile
            {
                Url = path,
                TextContent = text,
                FileName = Path.GetFileNameWithoutExtension(path),
                FileType = Path.GetExtension(path),
                DateLastSaved = File.GetLastWriteTime(path)
            };
        }

        public static SourceFile ParseExcel(string path)
        {
            throw new NotImplementedException("Excel files are not yet supported.");
        }

        public static SourceFile ParseCSV(string path)
        {
            throw new NotImplementedException("CSV files are not yet supported.");
        }

        public static SourceFile ParseText(string path)
        {
            string text = File.ReadAllText(path);
            return new SourceFile
            {
                Url = path,
                TextContent = text,

            };
        }

        //This should be modifed to include output from the webscraper perhaps or maybe simplified to take only paragraphs
        public static SourceFile ParseHTML(string path, bool removeHtmlTags = true)
        {
            HtmlDocument doc = new HtmlDocument();

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var web = new HtmlWeb();
                doc = web.Load(path);
            }
            else
            {
                doc.Load(path);
            }

            // Strip <script> and <style>
            doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            string text = removeHtmlTags
                ? Regex.Replace(doc.DocumentNode.InnerText, @"\s+", " ").Trim()
                : doc.DocumentNode.OuterHtml;

            return new SourceFile
            {
                Url = path,
                TextContent = text,
                DateLastSaved = File.GetLastWriteTime(path),

            };
        }

        public static SourceFile ParseHTML(string path)
        {
            return ParseHTML(path, true);
        }

        public static SourceFile ParseDocument(string path)
        {
            string extension = "";

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                Uri uri = new Uri(path);
                string fileName = Path.GetFileName(uri.LocalPath);
                extension = Path.GetExtension(fileName)?.ToLower();

                if (string.IsNullOrEmpty(extension) && (path.Contains("https:") || (path.Contains("http:"))))
                    extension = ".html";
            }
            else
            {
                extension = Path.GetExtension(path)?.ToLower();
            }

            if (SupportedFileTypes.ContainsKey(extension))
            {
                return SupportedFileTypes[extension](path);
            }

            throw new NotSupportedException($"File type '{extension}' is not supported.");
        }

        public static List<SourceFile> ParseFolder(string folderPath)
        {

            var results = new List<SourceFile>();

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Folder does not exist: {folderPath}");
                return results;
            }

            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                string extension = Path.GetExtension(file).ToLower();

                if (SupportedFileTypes.ContainsKey(extension))
                {
                    try
                    {
                        var result = ParseDocument(file);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse '{file}': {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Unsupported file type: {extension} - File: {file}");
                }
            }

            return results;
        }
    }
}



