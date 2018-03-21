using HtmlAgilityPack;
using LanguageExt;
using System;
using System.Linq;
using System.Text;

namespace Gatherer.Sites
{
    static class GuardianSite
    {
        public static Descriptor Guardian
            => new Descriptor
            {
                Type = SiteType.GUARDIAN,
                SymbolLimit = 600 * 5,
                Name = "Guardian",
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
                      a.Attributes["class"].Value == "fc-item__link"
                select a.Attributes["href"].Value;
            return new Lst<string>(newLinks);
        };
        private static Try<string> GetContent(HtmlDocument doc) => () =>
        {
            // Get headline
            HtmlNode headline =
                doc.DocumentNode
                .SelectSingleNode("//h1[contains(@class, 'content__headline')]");
            string header = headline?.InnerText ?? "";

            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in doc.DocumentNode
                    .SelectNodeList("//div[contains(@class,'content__article-body')]/p")
                 select p.InnerText));

            return header + Environment.NewLine + text;
        };
    }
}
