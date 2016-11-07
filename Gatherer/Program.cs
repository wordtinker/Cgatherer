using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.IO;

namespace Gatherer
{
    enum SiteType
    {
        GUARDIAN,
        REGISTER,
        INSIDER
    }

    abstract class Site
    {
        // Members
        protected string basePage;
        protected string section;

        // Properties

        /// <summary>
        /// Minimum amount of symbols in the article to be gathered.
        /// </summary>
        public abstract int SymbolLimit { get; }
        public abstract string Name { get; }
        public abstract Encoding PageEncoding { get; }
        public abstract string Language { get; }

        // Constructors 
        public Site(string basePage, string section)
        {
            this.basePage = basePage;
            this.section = section;
        }

        // Methods
        protected HtmlDocument GetPage(string url)
        {
            Console.WriteLine("Getting {0}", url);
            HtmlWeb htmlWeb = new HtmlWeb() {
                AutoDetectEncoding =false,
                OverrideEncoding = PageEncoding
            };
            HtmlDocument htmlDocument = htmlWeb.Load(url);
            return htmlDocument;
        }

        public abstract IEnumerable<string> GetNewArticles();
        public abstract bool GetContent(out string content);
    }

    class Insider : Site
    {
        public override int SymbolLimit { get { return 600 * 5; } }
        public override string Name { get { return "Insider"; } }
        public override Encoding PageEncoding { get { return Encoding.UTF8; } }
        public override string Language { get { return "English"; } }

        public Insider(string basePage, string section) : base(basePage, section) { }

        public override IEnumerable<string> GetNewArticles()
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            var newLinks = from a in htmlDocument.DocumentNode.Descendants("a")
                           where a.Attributes.Contains("href") &&
                                 a.Attributes.Contains("class") &&
                                 a.Attributes["class"].Value == "title" &&
                                 a.Attributes["href"].Value.Contains("www.businessinsider.com")
                           select a.Attributes["href"].Value;
            return newLinks;
        }

        public override bool GetContent(out string content)
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            // Get headline
            HtmlNode headline = htmlDocument.DocumentNode
                .SelectSingleNode("//div[contains(@class, 'sl-layout-post')]/h1");
            string header = headline?.InnerText ?? "";

            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in htmlDocument.DocumentNode
                    .SelectNodeList("//div[contains(@class,'KonaBody post-content')]/p")
                 select p.InnerText));

            content = header + Environment.NewLine + text;
            Console.WriteLine("Content length: {0}", content.Length);
            return content.Length >= SymbolLimit;
        }
    }

    class Guardian : Site
    {
        public override int SymbolLimit { get { return 600 * 5; } }
        public override string Name { get { return "Guardian"; } }
        public override Encoding PageEncoding { get { return Encoding.UTF8; } }
        public override string Language { get { return "English"; } }

        public Guardian(string basePage, string section) : base(basePage, section) { }

        public override IEnumerable<string> GetNewArticles()
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            var newLinks = from a in htmlDocument.DocumentNode.Descendants("a")
                           where a.Attributes.Contains("href") &&
                                 a.Attributes.Contains("class") &&
                                 a.Attributes["class"].Value == "fc-item__link"
                           select a.Attributes["href"].Value;
            return newLinks;
        }

        public override bool GetContent(out string content)
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            // Get headline
            HtmlNode headline = htmlDocument.DocumentNode
                .SelectSingleNode("//h1[contains(@class, 'content__headline')]");
            string header = headline?.InnerText ?? "";

            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in htmlDocument.DocumentNode
                    .SelectNodeList("//div[contains(@class,'content__article-body')]/p")
                 select p.InnerText));

            content = header + Environment.NewLine + text;
            Console.WriteLine("Content length: {0}", content.Length);
            return content.Length >= SymbolLimit;
        }
    }

    class Register : Site
    {
        public override int SymbolLimit { get { return 300 * 5; } }
        public override string Name { get { return "Register"; } }
        public override Encoding PageEncoding { get { return Encoding.UTF8; } }
        public override string Language { get { return "english"; } }

        public Register(string basePage, string section) : base(basePage, section) { }

        public override IEnumerable<string> GetNewArticles()
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            var newLinks = from a in htmlDocument.DocumentNode.Descendants("a")
                           where a.Attributes.Contains("href") &&
                                 a.Attributes.Contains("class") &&
                                 a.Attributes["class"].Value == "story_link" &&
                                 a.Attributes["href"].Value.Contains("www.theregister.co.uk")
                           select a.Attributes["href"].Value;
            return newLinks;
        }

        public override bool GetContent(out string content)
        {
            HtmlDocument htmlDocument = GetPage(basePage + section);
            // Get headline
            HtmlNode headline = htmlDocument.DocumentNode
                .SelectSingleNode("//div[contains(@class, 'article_head')]/h1");
            string header = headline?.InnerText ?? "";
            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in htmlDocument.DocumentNode
                    .SelectNodeList("//div[@id='body']/p")
                 select p.InnerText));

            content = header + Environment.NewLine + text;
            // Get multipage content
            HtmlNode newPage = htmlDocument.DocumentNode
                .SelectSingleNode("//div[@id='nextpage']/a[@href]");
            if (newPage != null)
            {
                Register nextPage = new Register(basePage, newPage.Attributes["href"].Value);
                string nextPageContent = null;
                nextPage.GetContent(out nextPageContent);
                content = content + Environment.NewLine + nextPageContent;
            }
            Console.WriteLine("Content length: {0}", content.Length);
            return content.Length >= SymbolLimit;
        }
    }

    static class SiteFactory
    {
        public static Site CreateSite(SiteType siteType, string basePage, string section="")
        {
            Site site = null;
            switch (siteType)
            {
                case SiteType.GUARDIAN:
                    site = new Guardian(basePage, section);
                    break;
                case SiteType.REGISTER:
                    site = new Register(basePage, section);
                    break;
                case SiteType.INSIDER:
                    site = new Insider(basePage, section);
                    break;
                default:
                    throw new NotImplementedException("Wrong site type");
            }
            return site;
        }
    }

    static class Extensions
    {
        public static IEnumerable<HtmlNode> SelectNodeList(this HtmlNode node, string xPath)
        {
            return node.SelectNodes(xPath)?.Nodes() ?? new List<HtmlNode>();
        }
    }

    class Program
    {
        private static List<KeyValuePair<SiteType, string>> siteList = new List<KeyValuePair<SiteType, string>>
            {
                new KeyValuePair<SiteType, string>(SiteType.GUARDIAN, "http://www.theguardian.com/uk-news"),
                new KeyValuePair<SiteType, string>(SiteType.REGISTER, "http://www.theregister.co.uk/"),
                new KeyValuePair<SiteType, string>(SiteType.INSIDER, "http://www.businessinsider.com/")
            };

        private static DB database = new DB("projects.db");

        private static void MainMethod()
        {
            // 1. Gather new links
            foreach (var item in siteList)
            {
                Site site = SiteFactory.CreateSite(item.Key, item.Value);
                List<string> newLinks = site.GetNewArticles().ToList();
                foreach (string link in newLinks)
                {
                    database.AddPAge((int)item.Key, link);
                }
                Console.WriteLine("Got {0} new links.", newLinks.Count);
            }

            // 2. Rip articles from fresh links
            foreach (Record rec in database.GetUnvisistedPages())
            {
                Site site = SiteFactory.CreateSite((SiteType)rec.ProjectId, rec.Link);
                string content = null;
                if (site.GetContent(out content))
                {
                    // save to file
                    string dirName = Path.Combine(site.Language, "corpus", site.Name);
                    string fileName = Path.Combine(dirName, rec.Id.ToString() + ".txt");
                    Directory.CreateDirectory(dirName);
                    File.WriteAllText(fileName, content);
                    Console.WriteLine("Writing to DB.");
                }
                // update DB
                database.SetVisited(rec.Link);
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                MainMethod();
                Console.WriteLine("Every link is gathered.");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
