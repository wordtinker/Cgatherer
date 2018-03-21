using LanguageExt;
using static System.Console;
using static LanguageExt.Prelude;
using static Gatherer.Repository;
using static Gatherer.Sites.SiteFactory;
using static Gatherer.HTML;
using static Gatherer.IO;
using Gatherer.Sites;

namespace Gatherer
{
    class Program
    {
        private static Try<Unit> PrintSavedArticles(int n) => () =>
        {
            WriteLine("Got {0} new articles.", n);
            return unit;
        };

        private static Try<Unit> PrintGatheredLinks(int n) => () =>
        {
            WriteLine("Got {0} new links.", n);
            return unit;
        };

        // with early return
        private static Try<int> UpdateArticles(Lst<(IDescriptor desc, Option<string> content)> articles) => () =>
        {
            articles.Iter(t => SetVisited(t.desc.URI));
            return articles.Count;
        };

        // with early return
        private static Try<Lst<(IDescriptor desc, Option<string> content)>> SaveArticles(Lst<(IDescriptor desc, Option<string> content)> articles) => () =>
        {
            articles
            .Iter(t =>
            {
                t.content.Match(
                    Some: val => Combine(t.desc.Language, t.desc.Name, t.desc.Id.ToString())
                                 .Bind(path => WriteFile(path, val)),
                    None: () => { });
            });
            return articles;
        };

        private static Try<Lst<(IDescriptor desc, Option<string> content)>> SeparateContent(Lst<(IDescriptor desc, string content)> articles) => ()
            => articles.Map(t
                => t.content.Length >= t.desc.SymbolLimit
                    ? (t.desc, Some(t.content))
                    : (t.desc, None));

        // with early return
        private static Try<int> AddPages(Lst<(IDescriptor desc, string link)> links) => () =>
        {
            links.Iter(t
                => AddPage((int)t.desc.Type, t.link)
                    .Match(
                        Fail: ex => throw ex,
                        Succ: _ => _));
            return links.Count;
        };

        // with early return
        private static Try<Lst<(IDescriptor desc, string content)>> DescriptorsToContent(Lst<IDescriptor> descriptors) => ()
            => descriptors.Map(d
                => GetContent(d.URI, d.PageEncoding, d.GetContent)
                   .Match(
                       Fail: ex => throw ex,
                       Succ: content => (d, content)));

        // with early return
        private static Try<Lst<(IDescriptor desc, string link)>> DescriptorsToLinks(Lst<IDescriptor> descriptors) => ()
            => descriptors.Bind(d 
                => GetContent(d.URI, d.PageEncoding, d.GatherArticles)
                   .Match(
                       Fail: ex => throw ex,
                       Succ: links => links)
                   .Map(links => (d, links)));

        // with early return
        private static Try<Lst<IDescriptor>> ListToDescriptors(Lst<(SiteType Type, string URI, string Section)> sites) => ()
            => sites.Map(item
                => ToSiteDescriptor(item.Type, item.URI, item.Section)
                   .Match(
                       Fail: ex => throw ex,
                       Succ: desc => desc));

        // with early return
        private static Try<Lst<IDescriptor>> ListToDescriptors(Lst<(int Id, int ProjectId, string Link)> sites) => ()
            => sites.Map(item
                => ToSiteDescriptor((SiteType)item.ProjectId, item.Link, id: item.Id)
                   .Match(
                       Fail: ex => throw ex,
                       Succ: desc => desc));

        private static Try<int> GatherNewLinks(Lst<(SiteType Type, string URI, string Section)> sites)
            => ListToDescriptors(sites)
               .Bind(DescriptorsToLinks)
               .Bind(AddPages);

        private static Try<int> ReadArticles()
            => GetUnvisistedPages()
               .Bind(ListToDescriptors)
               .Bind(DescriptorsToContent)
               .Bind(SeparateContent)
               .Bind(SaveArticles)
               .Bind(UpdateArticles);

        private static Try<Unit> MainMethod(Lst<(SiteType, string, string)> sites)
            => InitializeTables()
               .Bind(_ => GatherNewLinks(sites))
               .Bind(PrintGatheredLinks)
               .Bind(_ => ReadArticles())
               .Bind(PrintSavedArticles);
        
        static void Main(string[] args)
        {
            Lst<(SiteType Type, string URI, string Section)> sites = List
            (
                (SiteType.GUARDIAN, "http://www.theguardian.com", "/uk-news"),
                (SiteType.INSIDER, "http://www.businessinsider.com/", "")
            );

            MainMethod(sites).Match(
                Fail: ex =>
                {
                    WriteLine(ex.Message);
                    WriteLine(ex.StackTrace);
                },
                Succ: _ =>
                {
                    WriteLine("Every link is gathered.");
                    ReadKey();
                });
            // TODO test
            // TODO test throwing from low levels
        }
    }
}
