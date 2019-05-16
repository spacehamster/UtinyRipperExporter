using CommandLine;
using System.Diagnostics;
using uTinyRipper;
namespace Extract
{
    class Program
    {
        public class Options
        {
            [Option('g', "gamedir", Required = true, HelpText = "Set the root directiory where unity assets are located.")]
            public string GameDir { get; set; }
            [Option('f', "file", Required = false, HelpText = "Set the file to extract")]
            public string File { get; set; }
            [Option('o', "output", Required = true, HelpText = "Set the directory to output files")]
            public string ExportDir { get; set; }
            [Option('c', "guidbycontent", Required = false, HelpText = "Generate guid by content")]
            public bool GUIDByContent { get; set; }
            [Option('d', "exportdependencies", Default = true, Required = false, HelpText = "Export dependencies")]
            public bool ExportDependencies { get; set; }
            [Option('s', "scripts", Default = false, Required = false, HelpText = "Export scripts")]
            public bool ExportScripts { get; set; }
        }
        static void Main(string[] args)
        {
            Logger.Instance = new ConsoleLogger("log.txt");
            Config.IsAdvancedLog = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    Config.IsGenerateGUIDByContent = o.GUIDByContent;
                    Config.IsExportDependencies = o.ExportDependencies;
                    if (o.ExportScripts)
                    {
                        ScriptExporter.ExportAll(o.GameDir, o.ExportDir);
                    } else if (o.File == null){
                        GameStructureExporter.ExportGameStructure(o.GameDir, o.ExportDir);
                    } else if(o.File.EndsWith(".dll"))
                    {
                        ScriptExporter.ExportDLL(o.GameDir, o.File, o.ExportDir);
                    } else
                    {
                        GameStructureExporter.ExportBundles(o.GameDir, new string[] { o.File }, o.ExportDir, true);
                    }
                });
            sw.Stop();
            Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Elapsed={Util.FormatTime(sw.Elapsed)}");
        }
    }
}
