using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.SerializedFiles;
using uTinyRipperGUI.Exporters;
using DateTime = System.DateTime;
using Version = uTinyRipper.Version;

namespace Extract
{
	public class GameStructureExporter
	{
		string GameDir;
		string ExportPath;
		ExportOptions options;
		public GameStructure GameStructure = null;
		HashSet<string> m_LoadedFiles = new HashSet<string>();
		Func<uTinyRipper.Classes.Object, bool> Filter;
		public GameStructureExporter(ExportSettings settings, List<string> files, Func<uTinyRipper.Classes.Object, bool> filter = null)
		{
			GameDir = settings.GameDir;
			ExportPath = settings.ExportDir;
			options = new ExportOptions()
			{
				Platform = Platform.NoTarget,
				Flags = TransferInstructionFlags.NoTransferInstructionFlags,
			};
			GameStructure = GameStructure.Load(files);

			if (string.IsNullOrEmpty(settings.ExportVersion))
			{
				//The version in unity default resources and unity_builtin_extra seem to differ from the game version
				var versionCheckFile = GameStructure.FileCollection.Files.FirstOrDefault(f => !Path.GetFileName(f.Name).Contains("unity"));
				if (versionCheckFile != null)
				{
					options.Version = versionCheckFile.Version;
					Logger.Log(LogType.Info, LogCategory.Export, $"Version detected as {options.Version.ToString()}");
				}
				else
				{
					Logger.Log(LogType.Warning, LogCategory.Export, $"Could not detect version");
				}
			}
			else
			{
				var version = new Version();
				version.Parse(settings.ExportVersion);
				Logger.Log(LogType.Info, LogCategory.Export, $"Version set to {version.ToString()}");
			}
			if (filter != null)
			{
				Filter = filter;
			}
			else
			{
				Filter = (obj) => true;
			}
			var fileCollection = GameStructure.FileCollection;
			var textureExporter = new TextureAssetExporter();
			var engineExporter = new EngineAssetExporter();
			fileCollection.Exporter.OverrideExporter(ClassIDType.Material, engineExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Mesh, engineExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Shader, new CustomShaderAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.TextAsset, new TextAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.AudioClip, new AudioAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.Font, new FontAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.MovieTexture, new MovieTextureAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.Texture2D, textureExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Cubemap, textureExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Sprite, engineExporter); //engine or texture exporter?
			fileCollection.Exporter.EventExportStarted += () =>
			{
				Logger.Log(LogType.Info, LogCategory.Export, "EventExportStarted");
				UpdateTitle($"EventExportStarted");
			};
			fileCollection.Exporter.EventExportPreparationStarted += () =>
			{
				Logger.Log(LogType.Info, LogCategory.Export, "EventExportPreparationStarted");
				UpdateTitle($"EventExportPreparationStarted");
			};
			fileCollection.Exporter.EventExportPreparationFinished += () =>
			{
				Logger.Log(LogType.Info, LogCategory.Export, "EventExportPreparationFinished");
				UpdateTitle($"EventExportPreparationFinished");
			};
			fileCollection.Exporter.EventExportFinished += () =>
			{
				Logger.Log(LogType.Info, LogCategory.Export, "EventExportFinished");
				UpdateTitle($"EventExportFinished");
			};
			fileCollection.Exporter.EventExportStarted += () =>
			{
				Logger.Log(LogType.Info, LogCategory.Export, "EventExportStarted");
				UpdateTitle($"EventExportStarted");
			};
			fileCollection.Exporter.EventExportProgressUpdated += (int number, int total) =>
			{
				UpdateTitle($"Exported {number / (float)total * 100:0.#} - {number} of {total}");
			};
			if (settings.ScriptByName)
			{
				foreach(var asset in fileCollection.FetchAssets())
				{
					if(asset is MonoScript script)
					{
						using (MD5 md5 = MD5.Create())
						{
							var data = md5.ComputeHash(Encoding.UTF8.GetBytes($"{script.AssemblyName}.{script.Namespace}.{script.ClassName}"));
							var newGuid = new Guid(data);
							Util.SetGUID(script, newGuid);
						}
					}
				}
			}
			if (settings.ShaderByName)
			{
				foreach (var asset in fileCollection.FetchAssets())
				{
					if (asset is Shader shader)
					{
						using (MD5 md5 = MD5.Create())
						{
							var data = md5.ComputeHash(Encoding.UTF8.GetBytes($"{shader.ValidName}"));
							var newGuid = new Guid(data);
							var engGuid = new EngineGUID(newGuid);
							Util.SetGUID(shader, newGuid);
							Console.WriteLine($"Set shader {shader.ValidName} to Guid {engGuid}");
						}
					}
				}
			}
		}
		public void Export()
		{
			Util.PrepareExportDirectory(ExportPath);
			var assets = GameStructure.FileCollection.FetchAssets().Where(Filter);
			GameStructure.FileCollection.Exporter.Export(ExportPath,
				GameStructure.FileCollection,
				assets,
				options);
		}
		private void ExportBundles(IEnumerable<string> requestedPaths)
		{
			Util.PrepareExportDirectory(ExportPath);
			var requestedFiles = new HashSet<ISerializedFile>();
			foreach (var path in requestedPaths)
			{
				var file = Util.FindFile(GameStructure.FileCollection, path);
				requestedFiles.Add(file);
			}
			var assets = GameStructure.FileCollection.FetchAssets().Where((obj) => true || requestedFiles.Contains(obj.File) && Filter(obj));
			GameStructure.FileCollection.Exporter.Export(ExportPath,
				GameStructure.FileCollection,
				assets,
				options);
		}
		static DateTime lastUpdate = DateTime.Now - TimeSpan.FromDays(1);
		public static void UpdateTitle(string text, params string[] arguments)
		{
			var now = DateTime.Now;
			var delta = now - lastUpdate;
			if (delta.TotalMilliseconds > 500)
			{
				Console.Title = string.Format(text, arguments);
				lastUpdate = now;
			}
		}
		public static void ExportGameStructure(ExportSettings settings, Func<uTinyRipper.Classes.Object, bool> filter = null)
		{
			new GameStructureExporter(settings, new List<string>() { settings.GameDir }, filter).Export();
		}
		public static string ResolveBundleDepndency(string gameDir, string manifestPath, string bundleName)
		{
			var path = Util.NormalizePath(Path.Combine(Path.GetDirectoryName(manifestPath), bundleName));
			if (!File.Exists(path))
			{
				throw new FileNotFoundException($"Couldn't find bundle dependency {bundleName} for {manifestPath}");
			}
			return path;
		}
		public static List<string> GetBundleDependencies(string gameDir, IEnumerable<string> assetPaths)
		{
			var toExportHashSet = new HashSet<string>(assetPaths.Select(p => Util.NormalizePath(p)));
			var queue = new Queue<string>(assetPaths);
			while (queue.Count > 0)
			{
				var currentAssetPath = queue.Dequeue();
				currentAssetPath = Util.NormalizePath(currentAssetPath);
				toExportHashSet.Add(currentAssetPath);
				if (File.Exists($"{currentAssetPath}.manifest"))
				{
					var toCheckList = Util.
						GetManifestDependencies($"{currentAssetPath}.manifest")
						.Select(name => ResolveBundleDepndency(gameDir, $"{currentAssetPath}.manifest", name));
					foreach (var toCheckPath in toCheckList)
					{
						if (!toExportHashSet.Contains(toCheckPath))
						{
							queue.Enqueue(toCheckPath);
						}
					}
				}
			}
			return toExportHashSet.ToList();
		}
		public static void ExportBundles(ExportSettings settings, IEnumerable<string> assetPaths, bool loadBundleDependencies = true, Func<uTinyRipper.Classes.Object, bool> filter = null)
		{
			var exportPath = settings.ExportDir;
			var gameDir = settings.GameDir;
			Util.PrepareExportDirectory(exportPath);
			List<string> toExportList = null;
			if (loadBundleDependencies) {
				toExportList = GetBundleDependencies(gameDir, assetPaths);
			} else
			{
				toExportList = assetPaths.ToList();
			}
			toExportList.Add($"{gameDir}/Managed");
			Console.WriteLine($"Exporting Files:\n{string.Join("\n", toExportList)}");
			new GameStructureExporter(settings, toExportList, filter).ExportBundles(assetPaths);
		}
	}
}
