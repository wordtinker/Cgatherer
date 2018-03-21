using System;
using static Gatherer.Sites.InsiderSite;
using static Gatherer.Sites.GuardianSite;
using LanguageExt;
using HtmlAgilityPack;
using System.Text;
using System.Collections.Generic;

namespace Gatherer.Sites
{
    static class Extensions
    {
        public static IEnumerable<HtmlNode> SelectNodeList(this HtmlNode node, string xPath)
        {
            return node.SelectNodes(xPath)?.Nodes() ?? new List<HtmlNode>();
        }
    }
    enum SiteType
    {
        GUARDIAN,
        INSIDER
    }
    interface IDescriptor
    {
        SiteType Type { get; set; }
        int Id { get; set; }
        string BasePage { get; set; }
        string Section { get; set; }
        string URI { get; }
        /// <summary>
        /// Minimum amount of symbols in the article to be gathered.
        /// </summary>
        int SymbolLimit { get; set; }
        string Name { get; set; }
        Encoding PageEncoding { get; set; }
        string Language { get; set; }
        Func<HtmlDocument, Try<Lst<string>>> GatherArticles { get; set; }
        Func<HtmlDocument, Try<string>> GetContent { get; set; }
    }
    class Descriptor : IDescriptor
    {
        public SiteType Type { get; set; }
        public int Id { get; set; }
        public string BasePage { get; set; }
        public string Section { get; set; }
        public string URI => BasePage + Section;
        public int SymbolLimit { get; set; }
        public string Name { get; set; }
        public Encoding PageEncoding { get; set; }
        public string Language { get; set; }
        public Func<HtmlDocument, Try<Lst<string>>> GatherArticles { get; set; }
        public Func<HtmlDocument, Try<string>> GetContent { get; set; }
    }
    static class SiteFactory
    {
        public static Try<IDescriptor> ToSiteDescriptor(SiteType siteType, string basePage, string section = default, int id = default) => () =>
        {
            Descriptor site = null;
            switch (siteType)
            {
                case SiteType.GUARDIAN:
                    site = Guardian;
                    break;
                case SiteType.INSIDER:
                    site = Insider;
                    break;
                default:
                    throw new NotImplementedException("Wrong site type");
            }
            site.BasePage = basePage;
            site.Section = section;
            site.Id = id;
            return site;
        };
    }
}
