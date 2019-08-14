using CommandLine;
using System.Diagnostics;
using System.IO;
using uTinyRipper;
namespace Extract
{
	class Program
	{
		static void Main(string[] args)
		{
			Logger.Instance = new ConsoleLogger("log.txt");
			Config.IsAdvancedLog = true;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Parser.Default.ParseArguments<ExportSettings>(args)
				.WithParsed<ExportSettings>(o =>
				{
					Config.IsGenerateGUIDByContent = o.GUIDByContent;
					Config.IsExportDependencies = o.ExportDependencies;
					if (o.ExportScripts)
					{
						ScriptExporter.ExportAll(o.GameDir, o.ExportDir, o.ScriptByName);
					}
					else if (o.DumpInfo)
					{
						DumpInfo.DumpAllFileInfo(o.GameDir, o.ExportDir);
					}
					else if (o.File == null)
					{
						GameStructureExporter.ExportGameStructure(o);
					}
					else if (o.File.EndsWith(".dll"))
					{
						ScriptExporter.ExportDLL(o.GameDir, o.File, o.ExportDir, o.ScriptByName);
					}
					else
					{
						GameStructureExporter.ExportBundles(o, new string[] { o.File }, true);
					}
					if (o.FixScripts)
					{
						var dirName = Path.GetFileName(o.GameDir);
						if(dirName == "Kingmaker_Data")
						{
							ScriptFixer.FixKingmakerScripts(o.ExportDir);
						}
						if(dirName == "PillarsOfEternityII_Data")
						{
							ScriptFixer.FixPoE2Scripts(o.ExportDir);
						}
					}
				});
			sw.Stop();
			Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Elapsed={Util.FormatTime(sw.Elapsed)}");
		}
	}
}
