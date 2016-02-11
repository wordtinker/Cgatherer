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
        REGISTER
    }

    abstract class Site
    {
        // Members
        protected string section;

        // Properties

        /// <summary>
        /// Minimum amount of words in the article to be gathered.
        /// </summary>
        public abstract int WordLimit { get; }
        public abstract string Name { get; }
        public abstract string BasePage { get; }
        public abstract Encoding PageEncoding { get; }
        public abstract string Language { get; }

        // Constructors 
        public Site(string section)
        {
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

    class Guardian : Site
    {
        public override int WordLimit { get { return 600; } }
        public override string Name { get { return "Guardian"; } }
        public override string BasePage { get { return "http://www.theguardian.com"; } }
        public override Encoding PageEncoding { get { return Encoding.UTF8; } }
        public override string Language { get { return "English"; } }

        public Guardian(string section) : base(section) { }

        public override IEnumerable<string> GetNewArticles()
        {
            HtmlDocument htmlDocument = GetPage(BasePage + section);
            var newLinks = from a in htmlDocument.DocumentNode.Descendants("a")
                           where a.Attributes.Contains("href") &&
                                 a.Attributes.Contains("class") &&
                                 a.Attributes["class"].Value == "fc-item__link"
                           select a.Attributes["href"].Value;
            return newLinks;
        }

        public override bool GetContent(out string content)
        {
            HtmlDocument htmlDocument = GetPage(section);
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
            return content.Length >= WordLimit;
        }
    }

    class Register : Site
    {
        public override int WordLimit { get { return 300; } }
        public override string Name { get { return "Register"; } }
        public override string BasePage { get { return "http://www.theregister.co.uk/"; } }
        public override Encoding PageEncoding { get { return Encoding.UTF8; } }
        public override string Language { get { return "english"; } }

        public Register(string section) : base(section) { }

        public override IEnumerable<string> GetNewArticles()
        {
            // TODO
            throw new NotImplementedException();
        }

        public override bool GetContent(out string content)
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    static class SiteFactory
    {
        public static Site CreateSite(SiteType siteType, string section)
        {
            Site site = null;
            switch (siteType)
            {
                case SiteType.GUARDIAN:
                    site = new Guardian(section);
                    break;
                case SiteType.REGISTER:
                    site = new Register(section);
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
                new KeyValuePair<SiteType, string>(SiteType.GUARDIAN, "/uk-news"),
                //new KeyValuePair<SiteType, string>(SiteType.REGISTER, "")
                // TODO
            };

        private static DB database = new DB("projects.db");

        private static void MainMethod()
        {
            // 1. Gather new links
            foreach (var item in siteList)
            {
                Site site = SiteFactory.CreateSite(item.Key, item.Value);
                foreach (string link in site.GetNewArticles())
                {
                    database.AddPAge((int)item.Key, link);
                }
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
                }
                // update DB
                database.SetVisited(rec.Link);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                MainMethod();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
