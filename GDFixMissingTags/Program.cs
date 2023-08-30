using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace GDFixMissingTags
{
    internal class Program
    {

        private static readonly string CWD = Application.StartupPath;

        private static void Exit(string msg, int errCode = 0)
        {
            Console.WriteLine(msg);
            Console.ReadLine();
            Environment.Exit(errCode);
        }

        private struct LocaleData
        {
            public string InFilePath;
            public string OutDirPath;
            public Dictionary<string, string> Tags;
            public Dictionary<string, string> MissingTags;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
                Exit("Drag and drop zip files into the exe. Make sure to include Text_EN.zip as well!");

            var masterFileName = "Text_EN";
            var masterFileFound = false;
            foreach(var file in args)
            {
                if (!File.Exists(file))
                    Exit($"File '{file}' not found");
                if (Path.GetFileNameWithoutExtension(file) == masterFileName)
                    masterFileFound = true;
            }
            if (!masterFileFound)
                Exit("Text_EN.zip not included");

            string tmpPath = Path.GetTempPath() + Guid.NewGuid();
            Directory.CreateDirectory(tmpPath);

            var dataList = new Dictionary<string, LocaleData>();

            foreach (var zip in args)
            {
                if (Path.GetExtension(zip) != ".zip")
                {
                    Console.WriteLine($"Skipping '{zip}' - not a zip");
                    continue;
                }

                var fileNameNoExt = Path.GetFileNameWithoutExtension(zip);
                var extractPath = Path.Combine(tmpPath, fileNameNoExt);
                Console.WriteLine($"Extracting: {fileNameNoExt}");
                ZipFile.ExtractToDirectory(zip, extractPath);
                Console.WriteLine($"Reading tags: {fileNameNoExt}");

                dataList[fileNameNoExt] = new LocaleData {
                    InFilePath = zip,
                    OutDirPath = extractPath,
                    Tags = new Dictionary<string, string>(),
                    MissingTags = new Dictionary<string, string>(),
                };

                foreach (var file in Directory.GetFiles(extractPath, "tags_*.txt"))
                {
                    foreach(var line in File.ReadAllLines(file))
                    {
                        var lineSplit = line.Split('=').ToList();
                        if (lineSplit.Count < 2)
                            continue;
                        var key = lineSplit[0].Trim();
                        lineSplit.RemoveAt(0);
                        var value = string.Join("=", lineSplit).Trim();
                        if (key.StartsWith("#"))
                            continue;
                        dataList[fileNameNoExt].Tags[key] = value;
                    }
                }
            }

            if (!dataList.ContainsKey(masterFileName))
            {
                Exit("Master file name not found in data list");
            }

            foreach(var lData in dataList)
            {
                if (lData.Key == masterFileName)
                    continue;
                foreach (var masterTagKVP in dataList[masterFileName].Tags)
                {
                    if (!lData.Value.Tags.ContainsKey(masterTagKVP.Key))
                        lData.Value.MissingTags.Add(masterTagKVP.Key, masterTagKVP.Value);
                }
                if (lData.Value.MissingTags.Count > 0)
                {
                    Console.WriteLine($"Writing: {lData.Key}");
                    var lines = new List<string>();
                    foreach(var kvp in lData.Value.MissingTags)
                    {
                        lines.Add($"{kvp.Key}={kvp.Value}");
                    }
                    File.WriteAllLines(Path.Combine(lData.Value.OutDirPath, "tags_missing.txt"), lines);
                    var outZip = Path.Combine(lData.Value.InFilePath);
                    if (File.Exists(outZip))
                        File.Delete(outZip);
                    ZipFile.CreateFromDirectory(lData.Value.OutDirPath, outZip);
                }
            }
            
            Exit("Done");
        }




    }
}
