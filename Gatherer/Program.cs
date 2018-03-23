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
        private static Try<Unit> SaveArticle(IDescriptor desc, string content)
            => Combine(desc.Language, desc.Name, desc.Id.ToString())
               .Match(
                    p => WriteFile(p, content),
                    ex => throw ex);

        // with early return
        private static Try<int> SaveAndUpdate(Seq<(IDescriptor desc, Option<string> content)> articles) => () =>
        {
            articles.Iter(t
                => t.content.Match(
                    Some: val =>
                    {
                        SaveArticle(t.desc, val).IfFailThrow();
                        SetVisited(t.desc.URI).IfFailThrow();
                        WriteLine("Got {0}", t.desc.URI);
                    },
                    None: () =>
                    {
                        SetVisited(t.desc.URI).IfFailThrow();
                        WriteLine("Skipped {0}", t.desc.URI);
                    }));
            return articles.Count;
        };

        private static Try<Seq<(IDescriptor desc, Option<string> content)>>
            SeparateContent(Seq<(IDescriptor desc, string content)> articles) => ()
            => articles.Map(t
                => t.content.Length >= t.desc.SymbolLimit
                    ? (t.desc, Some(t.content))
                    : (t.desc, None));

        // with early return
        private static Try<int> AddPages(Seq<(IDescriptor desc, string link)> links) => () =>
        {
            links.Iter(t => AddPage((int)t.desc.Type, t.link).IfFailThrow());
            return links.Count;
        };

        // with early return
        private static Try<Seq<(IDescriptor desc, string content)>> DescriptorsToContent(Seq<IDescriptor> descriptors) => ()
            => descriptors.Map(d
                => (d, GetContent(d.URI, d.PageEncoding, d.GetContent)
                       .IfFailThrow()));

        // with early return
        private static Try<Seq<(IDescriptor desc, string link)>> DescriptorsToLinks(Seq<IDescriptor> descriptors) => ()
            => Seq(descriptors.Bind(d
                => GetContent(d.URI, d.PageEncoding, d.GatherArticles)
                   .IfFailThrow()
                   .Map(links => (d, links))));

        // with early return
        private static Try<Seq<IDescriptor>> ListToDescriptors(Seq<(SiteType Type, string URI, string Section)> sites) => ()
            => sites.Map(item
                => ToSiteDescriptor(item.Type, item.URI, item.Section)
                   .IfFailThrow());

        // with early return
        private static Try<Seq<IDescriptor>> ListToDescriptors(Seq<(int Id, int ProjectId, string Link)> sites) => ()
            => sites.Map(item
                => ToSiteDescriptor((SiteType)item.ProjectId, item.Link, id: item.Id)
                   .IfFailThrow());

        private static Try<int> GatherNewLinks(Seq<(SiteType Type, string URI, string Section)> sites)
            => ListToDescriptors(sites)
               .Bind(DescriptorsToLinks)
               .Bind(AddPages);

        private static Try<int> ReadArticles()
            => ListToDescriptors(Seq(GetUnvisistedPages().IfFailThrow()))
               .Bind(DescriptorsToContent)
               .Bind(SeparateContent)
               .Bind(SaveAndUpdate);

        private static Try<Unit> MainMethod(Seq<(SiteType, string, string)> sites)
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

            MainMethod(Seq(sites)).Match(
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
        }
    }
}
