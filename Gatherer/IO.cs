using LanguageExt;
using System.IO;
using static LanguageExt.Prelude;

namespace Gatherer
{
    static class IO
    {
        public static Try<Unit> WriteFile(string filePath, string content) => () =>
        {
            string name = Path.ChangeExtension(filePath, ".txt");
            FileInfo file = new FileInfo(name);
            file.Directory.Create();
            File.WriteAllText(file.FullName, content);
            return unit;
        };
        public static Try<string> Combine(params string[] paths) => ()
            => Path.Combine(paths);
    }
}
