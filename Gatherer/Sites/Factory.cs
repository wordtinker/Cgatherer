using System;
using static Gatherer.Sites.GuardianSite;
using static Gatherer.Sites.ITBusinessSite;
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
        ITBUSINESS
    }
    interface IDescriptor
    {
        SiteType Type { get; }
        int Id { get; set; }
        string BasePage { get; set; }
        string Section { get; set; }
        string URI { get; }
        /// <summary>
        /// Minimum amount of symbols in the article to be gathered.
        /// </summary>
        int SymbolLimit { get; }
        string Name { get; }
        Encoding PageEncoding { get; }
        string Language { get; }
        Func<HtmlDocument, Try<Lst<string>>> GatherArticles { get; }
        Func<HtmlDocument, Try<string>> GetContent { get; }
    }
    class Descriptor : IDescriptor
    {
        public SiteType Type { get; }
        public int Id { get; set; }
        public string BasePage { get; set; } = String.Empty;
        public string Section { get; set; } = String.Empty;
        public string URI => BasePage + Section;
        public int SymbolLimit { get; }
        public string Name { get; }
        public Encoding PageEncoding { get; }
        public string Language { get; }
        public Func<HtmlDocument, Try<Lst<string>>> GatherArticles { get; }
        public Func<HtmlDocument, Try<string>> GetContent { get; }

        public Descriptor(SiteType type, int symbolLimit, string name, Encoding pageEncoding, string language,
            Func<HtmlDocument, Try<Lst<string>>> gatherArticles, Func<HtmlDocument, Try<string>> getContent)
        {
            Type = type;
            SymbolLimit = symbolLimit;
            Name = name;
            PageEncoding = pageEncoding;
            Language = language;
            GatherArticles = gatherArticles;
            GetContent = getContent;
        }
    }
    static class SiteFactory
    {
        public static Try<IDescriptor> ToSiteDescriptor(SiteType siteType, string basePage, string section = "", int id = default) => () =>
        {
            // TODO: see C# 8.0 switch expression when it's ready
            // Descriptor site = siteType switch {patt => res, , _ => throw}
            Descriptor site = null;
            switch (siteType)
            {
                case SiteType.GUARDIAN:
                    site = Guardian;
                    break;
                case SiteType.ITBUSINESS:
                    site = ITBusiness;
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
