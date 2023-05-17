using System;
using System.IO;
using System.Linq;

namespace TCPServer
{
    public static class Logger
    {
        public static void Log(LogInfoType infoType, string info)
        {
            string path = Handlers.Options.LogfilePath;
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            if (fileInfo.Exists)
            {
                if (fileInfo.Length > Handlers.Options.LogfileLimitMB)
                {
                    var lines = File.ReadAllLines(path);
                    File.WriteAllLines(path, lines.Skip(lines.Length / 2).ToArray()); //delete half of lines after file reached limit
                }
            }
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine($"{DateTime.Now} | {infoType.ToString().ToUpper()} | {info}");
                Console.WriteLine(info);
            }
        }
    }
    public enum LogInfoType
    {
        Info,
        Error
    }
}