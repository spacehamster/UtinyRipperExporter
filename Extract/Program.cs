using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.SerializedFiles;
using Object = uTinyRipper.Classes.Object;
using Version = uTinyRipper.Version;
namespace Extract
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance = new ConsoleLogger("log.txt");
            Config.IsAdvancedLog = true;
            Config.IsGenerateGUIDByContent = false;
            Config.IsExportDependencies = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //TODO export here
            Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Elapsed={Util.FormatTime(sw.Elapsed)}");
        }
    }
}
