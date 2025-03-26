using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LLama.Rag
{
    public interface IWebScraper
    {
        HashSet<string> VisitedUrls { get; }
        List<HtmlDocument> Documents { get; }

        public class WebsiteData
        {
            public string Url { get; set; }
            public HtmlDocument Document { get; set; }
            public string Title { get; set; } = string.Empty; //Tile of the web page if it exists
            public List<string> Links { get; set; } = new List<string>(); //List of links associated on the webpage
            public string Summary { get; set; } = string.Empty; //Summary of the webpage
            public float[] SummaryEmbedding { get; set; } = Array.Empty<float>(); // Embbeding for the summarized page for matching  relavent sources
            public string[] Keywords { get; set; } = Array.Empty<string>(); // Relavent keywords scraped from website
            public float[] KeyWordEmbeddings { get; set; } = Array.Empty<float>(); // Embedded Keywords for Searching for relavent sources

            //string Summarize(HtmlDocument document) { }

        }

        Task<List<string>> ExtractVisibleTextAsync(WebsiteData site, int minWordLength, bool checkSentences, bool explodeParagraphs);
        Task<List<string>> ExtractParagraphsAsync(WebsiteData site, bool explodeParagraphs);

        

        
    }
}