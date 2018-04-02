using System.IO;

namespace Gatherer
{
    static class IO
    {
        public static void WriteFile(string filePath, string content)
        {
            string name = Path.ChangeExtension(filePath, ".txt");
            FileInfo file = new FileInfo(name);
            file.Directory.Create();
            File.WriteAllText(file.FullName, content);
        }
        public static string Combine(params string[] paths)
            => Path.Combine(paths);
    }
}
