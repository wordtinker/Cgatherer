using HtmlAgilityPack;
using System.Text;

namespace Gatherer
{
    static class HTML
    {
        public static HtmlDocument GetPage(string url, Encoding encoding)
        {
            HtmlWeb htmlWeb = new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = encoding
            };
            return htmlWeb.Load(url);
        }
    }
}
