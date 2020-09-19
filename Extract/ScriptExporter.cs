using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using uTinyRipper;
using uTinyRipper.Classes;
using uTinyRipper.Converters;
using uTinyRipper.Converters.Script;
using uTinyRipper.Game;
using uTinyRipper.Game.Assembly;
using Object = uTinyRipper.Classes.Object;
using Version = uTinyRipper.Version;

namespace Extract
{
	public class ScriptExporter
	{
		string GameDir;
		GameCollection fileCollection;
		private string ExportPath;
		ExportOptions options;
		HashSet<string> m_LoadedFiles = new HashSet<string>();
		bool ScriptByName;
		public ScriptExporter(string gameDir, string exportPath, bool scriptByName)
		{
			GameDir = gameDir;
			ExportPath = exportPath;
			ScriptByName = scriptByName;
			options = new ExportOptions(
				new Version(2017, 3, 0, VersionType.Final, 3),
				Platform.NoTarget,
				TransferInstructionFlags.NoTransferInstructionFlags
			);
		}
		public static void ExportAll(string GameDir, string exportPath, bool scriptByName)
		{
			Util.PrepareExportDirectory(exportPath);
			var scriptExporter = new ScriptExporter(GameDir, exportPath, scriptByName);
			scriptExporter.DoExportAll();
		}
		public static void Export(string GameDir, string exportPath, bool scriptByName, Func<MonoScript, bool> filter = null)
		{
			if (filter == null) filter = ScriptSelector;
			Util.PrepareExportDirectory(exportPath);
			var scriptExporter = new ScriptExporter(GameDir, exportPath, scriptByName);
			scriptExporter.DoExport(filter);
		}
	   
		static bool ScriptSelector(Object asset)
		{
			if (asset is MonoScript) return true;
			return false;
		}
		private string RequestAssembly(string asm)
		{
			Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Requested Assembly {asm}");
			if (!File.Exists($@"{GameDir}\Managed\{asm}.dll"))
			{
				Logger.Instance.Log(LogType.Warning, LogCategory.Debug, $"Can't find {asm}");
				return null;
			}
			return $@"{GameDir}\Managed\{asm}.dll";
		}

		private void RequestDepency(string dep)
		{
			Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Requested Dependency {dep}");
			var searchPaths = new string[]
			{
				"",
				"Resources",
				"StreamingAssets"
			};
			if (!m_LoadedFiles.Contains(dep))
			{
				foreach (var path in searchPaths)
				{
					if (File.Exists($@"{GameDir}\{path}\{dep}"))
					{
						m_LoadedFiles.Add(dep);
						return;
					}
				}

			}
		}
		private string RequestResource(string dep)
		{
			Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Requested Resource {dep}");
			var searchPaths = new string[]
			{
				"",
				"Resources",
				"StreamingAssets"
			};
			foreach (var path in searchPaths)
			{
				if (File.Exists($@"{GameDir}\{path}\{dep}"))
				{
					return $@"{GameDir}\{path}\{dep}";
				}
			}
			throw new Exception($"Couldn't find resource path {dep}");
		}
		public static void ExportDLL(string GameDir, string dllPath, string exportPath, bool scriptByName)
		{
			Util.PrepareExportDirectory(exportPath);
			var scriptExporter = new ScriptExporter(GameDir, exportPath, scriptByName);
			scriptExporter.DoExportDLL(dllPath);

		}
		void DoExportDLL(string dllPath)
		{
			var fileCollection = Util.CreateGameCollection();
			var assemblyManager = (AssemblyManager)fileCollection.AssemblyManager;
			fileCollection.AssemblyManager.Load(dllPath);
			fileCollection.Exporter.Export(ExportPath, fileCollection, new SerializedFile[] { }, options);

			ScriptExportManager scriptManager = new ScriptExportManager(fileCollection.Layout, ExportPath);
			AssemblyDefinition myLibrary = AssemblyDefinition.ReadAssembly(dllPath);
			var refrences = myLibrary.MainModule.AssemblyReferences;
			foreach (TypeDefinition type in myLibrary.MainModule.Types)
			{
				//TODO: only export unity serializable classes
				if (!type.IsClass || type.Name == "<Module>") continue;
				var libName = myLibrary.Name.Name;
				var @namespace = type.Namespace;
				var className = type.Name;
				var scriptID = assemblyManager.GetScriptID(libName, @namespace, className);
				var exportType = assemblyManager.GetExportType(scriptManager, scriptID);
				scriptManager.Export(exportType);
			}
		}
		string GetMainAssetPath()
		{
			var assets = new string[]
			{
				"globalgamemanagers.assets",
				"mainData",
				"data.Unity3d"
			};
			foreach(var asset in assets)
			{
				var mainAssetPath = Path.Combine(GameDir, asset);
				if (File.Exists(mainAssetPath))
				{
					return mainAssetPath;
				}
			}
			throw new Exception("Could not find main asset file");
		}
		void DoExportAll()
		{
			Util.PrepareExportDirectory(ExportPath);
			var managedPath = Path.Combine(GameDir, "Managed");
			var mainAssetPath = GetMainAssetPath();
			var gameStructure = GameStructure.Load(new string[]
			{
				mainAssetPath,
				managedPath
			});
			fileCollection = gameStructure.FileCollection;
			var scripts = fileCollection.FetchAssets().Where(o => o is MonoScript ms).ToArray();
			foreach (Object asset in scripts)
			{
				MonoScript script = (MonoScript)asset;
				if (ScriptByName)
				{
					using (MD5 md5 = MD5.Create())
					{
						var data = md5.ComputeHash(Encoding.UTF8.GetBytes($"{script.AssemblyName}.{script.Namespace}.{script.ClassName}"));
						Util.SetGUID(script, data);
					}
				}
			}
			gameStructure.Export(ExportPath, asset => asset is MonoScript);
		}
		//Refer MonoManager, ScriptAssetExporter, ScriptExportManager
		void DoExport(Func<MonoScript, bool> selector = null)
		{
			var managedPath = Path.Combine(GameDir, "Managed");
			var globalgamemanagersPath = Path.Combine(GameDir, "globalgamemanagers.assets");
			var gameStructure = GameStructure.Load(new string[]
			{
				globalgamemanagersPath,
				managedPath
			});
			fileCollection = gameStructure.FileCollection;
			if (selector == null) selector = (o) => true;
			var assets = fileCollection.FetchAssets().Where(o => o is MonoScript ms && selector(ms)).ToArray();
			ScriptExportManager scriptManager = new ScriptExportManager(gameStructure.FileCollection.Layout, ExportPath);
			Dictionary<Object, ScriptExportType> exportTypes = new Dictionary<Object, ScriptExportType>();
			foreach (Object asset in assets)
			{
				MonoScript script = (MonoScript)asset;
				if (ScriptByName)
				{
					using (MD5 md5 = MD5.Create())
					{
						var data = md5.ComputeHash(Encoding.UTF8.GetBytes($"{script.AssemblyName}.{script.Namespace}.{script.ClassName}"));
						Util.SetGUID(script, data);
					}
				}
				ScriptExportType exportType = script.GetExportType(scriptManager);
				exportTypes.Add(asset, exportType);
			}
			foreach (KeyValuePair<Object, ScriptExportType> exportType in exportTypes)
			{
				string path = scriptManager.Export(exportType.Value);
			}
			//scriptManager.ExportRest();
		}
	}
}
