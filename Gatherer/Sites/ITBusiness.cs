﻿using HtmlAgilityPack;
using LanguageExt;
using System;
using System.Linq;
using System.Text;

namespace Gatherer.Sites
{
    static class ITBusinessSite
    {
        public static Descriptor ITBusiness
            => new Descriptor
            (
                type: SiteType.ITBUSINESS,
                symbolLimit: 600 * 5,
                name: "ITBusiness",
                pageEncoding: Encoding.UTF8,
                language: "English",
                gatherArticles: GatherArticles,
                getContent: GetContent
            );
        private static Try<Lst<string>> GatherArticles(HtmlDocument doc) => () =>
        {
            var newLinks =
                (from a in doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'list-story')]").Descendants("a")
                 where a.Attributes.Contains("href")
                 select a.Attributes["href"].Value).Distinct();
            return new Lst<string>(newLinks);
        };
        private static Try<string> GetContent(HtmlDocument doc) => () =>
        {
            // Get headline
            HtmlNode headline =
                doc.DocumentNode
                .SelectSingleNode("//h1[contains(@class, 'article-title')]");
            string header = headline?.InnerText ?? "";

            // Get article
            var text = string.Join(
                Environment.NewLine,
                (from p in doc.DocumentNode
                    .SelectNodeList("//div[contains(@class,'entry-content')]/p")
                 select p.InnerText));

            return HtmlEntity.DeEntitize(header + Environment.NewLine + text);
        };
    }
}
