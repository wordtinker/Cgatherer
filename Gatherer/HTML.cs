using HtmlAgilityPack;
using LanguageExt;
using System;
using System.Text;
using static System.Console;

namespace Gatherer
{
    static class HTML
    {
        public static Try<T>
            GetContent<T>(string uri, Encoding encoding,
                       Func<HtmlDocument, Try<T>> parserf)
            => GetPage(uri, encoding)
               .Bind(doc => parserf(doc));

        private static Try<HtmlDocument> GetPage(string url, Encoding encoding) => () =>
        {
            // that makes function impure
            // should be mitigated by pipeline ?
            // or by async progress handler ?
            WriteLine("Getting {0}", url);
            HtmlWeb htmlWeb = new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = encoding
            };
            return htmlWeb.Load(url);
        };
    }
}
