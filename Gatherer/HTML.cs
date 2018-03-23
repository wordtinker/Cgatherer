using HtmlAgilityPack;
using LanguageExt;
using System;
using System.Text;

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
            HtmlWeb htmlWeb = new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = encoding
            };
            return htmlWeb.Load(url);
        };
    }
}
