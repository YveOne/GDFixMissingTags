using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

using Utils;

namespace GDFixMissingTags
{
    internal class Program
    {

        #region common stuff

        private static readonly string CWD = Application.StartupPath;

        private static void Exit(string msg, int errCode = 0)
        {
            Console.WriteLine(msg);
            System.Threading.Thread.Sleep(5000);
            Environment.Exit(errCode);
        }

        #endregion

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
                if (Path.GetExtension(file) != ".zip")
                    Exit($"File '{file}' is not a zip");
                if (Path.GetFileNameWithoutExtension(file).ToLower().Contains("text_en"))
                    masterFileFound = true;
            }
            if (!masterFileFound)
                Exit("Text_EN.zip not included");

            string tmpPath = Funcs.GetTempFileName();
            Directory.CreateDirectory(tmpPath);

            var masterTags = new Dictionary<string, string>();
            var dataList = new Dictionary<string, LocaleData>();

            foreach (var zip in args)
            {
                var fileNameNoExt = Path.GetFileNameWithoutExtension(zip);
                var extractPath = Path.Combine(tmpPath, fileNameNoExt);
                Console.WriteLine($"Extracting: {fileNameNoExt}");
                ZipFile.ExtractToDirectory(zip, extractPath);
                Console.WriteLine($"Reading tags: {fileNameNoExt}");

                if (fileNameNoExt.ToLower().Contains("text_en"))
                {
                    foreach (var kvp in ReadDictFromDirectory(extractPath))
                        masterTags[kvp.Key] = kvp.Value;
                }
                else
                {
                    dataList[fileNameNoExt] = new LocaleData
                    {
                        InFilePath = zip,
                        OutDirPath = extractPath,
                        Tags = ReadDictFromDirectory(extractPath),
                        MissingTags = new Dictionary<string, string>(),
                    };
                }
            }

            if (masterTags.Count() == 0)
                Exit("No master tags found");

            foreach(var lData in dataList)
            {
                foreach (var masterTagKVP in masterTags)
                    if (!lData.Value.Tags.ContainsKey(masterTagKVP.Key))
                        lData.Value.MissingTags[masterTagKVP.Key] = masterTagKVP.Value;

                if (lData.Value.MissingTags.Count > 0)
                {
                    Console.WriteLine($"Writing: {lData.Key}");
                    var lines = new List<string>();
                    foreach(var kvp in lData.Value.MissingTags)
                        lines.Add($"{kvp.Key}={kvp.Value}");
                    var tagsMissingTxt = Path.Combine(lData.Value.OutDirPath, "tags_missing.txt");
                    File.AppendAllLines(tagsMissingTxt, lines);
                    var outZip = Path.Combine(lData.Value.InFilePath);
                    if (File.Exists(outZip))
                        File.Delete(outZip);
                    ZipFile.CreateFromDirectory(lData.Value.OutDirPath, outZip);
                }
            }
            Directory.Delete(tmpPath, true);
            Exit("Done");
        }

        private static Dictionary<string, string> ReadDictFromDirectory(string dirPath)
        {
            var dict = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(dirPath, "tags*.txt", SearchOption.AllDirectories))
            {
                foreach(var kvp in Funcs.ReadDictionaryFromFile(file))
                    dict[kvp.Key] = kvp.Value;
            }
            return dict;
        }

    }
}
