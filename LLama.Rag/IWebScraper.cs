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
            public string Title { get; set; }
            public List<string> Links { get; set; } = new List<string>();

        }

        Task<List<string>> ExtractVisibleTextAsync(WebsiteData site, int minWordLength, bool checkSentences, bool explodeParagraphs);
        Task<List<string>> ExtractParagraphsAsync(WebsiteData site, bool explodeParagraphs);

        

        
    }
}