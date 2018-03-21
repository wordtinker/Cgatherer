using HtmlAgilityPack;
using LanguageExt;
using System;
using System.Linq;
using System.Text;

namespace Gatherer.Sites
{
    static class InsiderSite
    {
        public static Descriptor Insider
            => new Descriptor
            {
                Type = SiteType.INSIDER,
                SymbolLimit = 600 * 2,
                Name = "Insider",
                PageEncoding = Encoding.UTF8,
                Language = "English",
                GatherArticles = GatherArticles,
                GetContent = GetContent
            };
        private static Try<Lst<string>> GatherArticles(HtmlDocument doc) => () =>
        {
            var newLinks =
                from a in doc.DocumentNode.Descendants("a")
                where a.Attributes.Contains("href") &&
                      a.Attributes.Contains("class") &&
                      a.Attributes["class"].Value == "title" &&
                      a.Attributes["href"].Value.Contains("www.businessinsider.com")
                select a.Attributes["href"].Value;
            return new Lst<string>(newLinks);
        };
        private static Try<string> GetContent(HtmlDocument doc) => () =>
        {
            // Get headline
            HtmlNode headline = doc.DocumentNode
                .SelectSingleNode("//div[contains(@class, 'sl-layout-post')]/h1");
            string header = headline?.InnerText ?? "";

            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in doc.DocumentNode
                    .SelectNodeList("//div[contains(@class,'KonaBody post-content')]/p")
                 select p.InnerText));

            return header + Environment.NewLine + text;
        };
    }
}
