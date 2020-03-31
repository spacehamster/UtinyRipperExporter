using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using uTinyRipper;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Misc;
using uTinyRipper.Converters;
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
		public GameStructureExporter(ExportSettings settings, IEnumerable<string> files)
		{
			GameDir = settings.GameDir;
			ExportPath = settings.ExportDir;

			GameStructure = GameStructure.Load(files);
			Version version = new Version(2017, 3, 0, VersionType.Final, 3);
			if (string.IsNullOrEmpty(settings.ExportVersion) || settings.ExportVersion.ToLower() == "detect")
			{
				//The version in unity default resources and unity_builtin_extra seem to differ from the game version
				version = GameStructure.FileCollection.GameFiles.Values.Max(t => t.Version);
				Logger.Log(LogType.Info, LogCategory.Export, $"Version detected as {version.ToString()}");
			}
			else
			{
				version = Version.Parse(settings.ExportVersion);
				Logger.Log(LogType.Info, LogCategory.Export, $"Version set to {version.ToString()}");
			}
			options = new ExportOptions(
				version,
				Platform.NoTarget,
				TransferInstructionFlags.NoTransferInstructionFlags
			);
			var fileCollection = GameStructure.FileCollection;
			var textureExporter = new TextureAssetExporter();
			var engineExporter = new EngineAssetExporter();
			fileCollection.Exporter.OverrideExporter(ClassIDType.Material, engineExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Mesh, engineExporter);
			fileCollection.Exporter.OverrideExporter(ClassIDType.Shader, new CustomShaderAssetExporter(settings));
			fileCollection.Exporter.OverrideExporter(ClassIDType.TextAsset, new TextAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.AudioClip, new AudioAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.Font, new FontAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.MovieTexture, new MovieTextureAssetExporter());
			fileCollection.Exporter.OverrideExporter(ClassIDType.Texture2D, engineExporter);
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
							Util.SetGUID(script, data);
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
							Util.SetGUID(shader, data);
							Console.WriteLine($"Set shader {shader.ValidName}");
						}
					}
				}
			}
		}
		public void Export(Func<uTinyRipper.Classes.Object, bool> filter = null)
		{
			Util.PrepareExportDirectory(ExportPath);
			if (filter != null) options.Filter = filter;
			GameStructure.FileCollection.Exporter.Export(ExportPath,
				GameStructure.FileCollection,
				Util.GetSerializedFiles(GameStructure.FileCollection),
				options);
		}
		public void ExportBundles(IEnumerable<string> requestedPaths, Func<uTinyRipper.Classes.Object, bool> filter = null)
		{
			Util.PrepareExportDirectory(ExportPath);
			if (filter != null)
			{
				options.Filter = filter;
			}
			var requestedFiles = new HashSet<ISerializedFile>();
			foreach (var path in requestedPaths)
			{
				var file = Util.FindFile(GameStructure.FileCollection, path);
				requestedFiles.Add(file);
			}
			var serializedFiles = Util.GetSerializedFiles(GameStructure.FileCollection);
			GameStructure.FileCollection.Exporter.Export(ExportPath,
				GameStructure.FileCollection,
				serializedFiles,
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
		public static void ExportGameStructure(ExportSettings settings, Func<uTinyRipper.Classes.Object, bool> filter = null, IEnumerable<string> extraFiles = null)
		{
			var files = new List<string>() { settings.GameDir };
			if (extraFiles != null) files.AddRange(extraFiles);
			new GameStructureExporter(settings, files).Export(filter);
		}
		public static GameStructureExporter Load(ExportSettings settings, IEnumerable<string> extraFiles = null)
		{
			var files = new List<string>() { settings.GameDir };
			if (extraFiles != null) files.AddRange(extraFiles);
			return new GameStructureExporter(settings, files);
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
		public static GameStructureExporter LoadBundles(ExportSettings settings, IEnumerable<string> assetPaths, bool loadBundleDependencies = true)
		{
			var exportPath = settings.ExportDir;
			var gameDir = settings.GameDir;
			Util.PrepareExportDirectory(exportPath);
			List<string> toExportList = null;
			if (loadBundleDependencies)
			{
				toExportList = GetBundleDependencies(gameDir, assetPaths);
			}
			else
			{
				toExportList = assetPaths.ToList();
			}
			toExportList.Add($"{gameDir}/Managed");
			Console.WriteLine($"Exporting Files:\n{string.Join("\n", toExportList)}");
			return new GameStructureExporter(settings, toExportList);
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
			new GameStructureExporter(settings, toExportList).ExportBundles(assetPaths, filter);
		}
	}
}
