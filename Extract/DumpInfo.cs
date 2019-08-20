using Extract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.ArchiveFiles;
using uTinyRipper.BundleFiles;
using uTinyRipper.Classes;
using uTinyRipper.ResourceFiles;
using uTinyRipper.SerializedFiles;
using uTinyRipper.WebFiles;
using Object = uTinyRipper.Classes.Object;

namespace Extract
{
	public class DumpInfo
	{
		static void DumpFileInfo(SerializedFile container, string exportPath)
		{
			Console.WriteLine($"Dumping container {container.Name }");
			Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
			using (var sw = new StreamWriter($"{exportPath}.txt"))
			{
				WriteFileInfo(container, sw);
				sw.WriteLine("");
				DumpObjectInfo(container.FetchAssets(), sw);
				if (container.Name == "globalgamemanagers")
				{
					BuildSettings buildSettings = (BuildSettings)container.FetchAssets().FirstOrDefault(asset => asset is BuildSettings);
					if (buildSettings != null)
					{
						sw.WriteLine("");
						DumpBuildSettings(buildSettings, sw);
					}
				}
			}
		}
		static void DumpFileListInfo(FileList fileList, string exportPath)
		{
			Console.WriteLine($"Dumping FileList {Path.GetFileName(exportPath)}");
			Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
			using (var sw = new StreamWriter($"{exportPath}.txt"))
			{
				if(fileList is BundleFile bf)
				{
					DumpBundleFileInfo(bf, sw);
				}
				if (fileList is ArchiveFile af)
				{
					//TODO
					sw.WriteLine($"ArchiveFile");
				}
				if (fileList is WebFile wf)
				{
					//TODO
					sw.WriteLine($"WebFile");
				}
				sw.WriteLine($"ResourceFile count {fileList.ResourceFiles.Count}");
				foreach (var resourceFile in fileList.ResourceFiles)
				{
					sw.WriteLine($"ResourceFile: Path {resourceFile.FilePath}, Name {resourceFile.Name}");
				}
				sw.WriteLine($"");
				sw.WriteLine($"SerializedFile count {fileList.SerializedFiles.Count}");
				foreach (var serializedFile in fileList.SerializedFiles)
				{
					sw.WriteLine("");
					WriteFileInfo(serializedFile, sw);
					sw.WriteLine("");
					DumpObjectInfo(serializedFile.FetchAssets(), sw);
				}
			}
		}
		static void DumpBundleFileInfo(BundleFile bundleFile, StreamWriter sw)
		{
			sw.WriteLine("BundleFile");
			var metadata = bundleFile.Metadata;
			var header = bundleFile.Header;
			sw.WriteLine("  Metadata: Entry.Key, Entry.Name, Entry.NameOrigin");
			foreach (var entry in metadata.Entries)
			{
				sw.WriteLine($"	{entry.Key}, {entry.Value.Name}, {entry.Value.NameOrigin}");
			}
			sw.WriteLine("  Header");
			sw.WriteLine($"	EngineVersion: {header.EngineVersion}");
			sw.WriteLine($"	Generation: {header.Generation}");
			sw.WriteLine($"	PlayerVersion: {header.PlayerVersion}");
			sw.WriteLine($"	Type: {header.Type}");
		}
		static void DumpObjectInfo(IEnumerable<Object> objects, StreamWriter sw)
		{
			sw.WriteLine("Name, ClassID, GUID, FileIndex, PathID, asset.IsValid, Exporter");
			foreach (var asset in objects)
			{
				var name = Util.GetName(asset);
				var pptr = asset.File.CreatePPtr(asset);
				sw.WriteLine($"{name}, {asset.ClassID}, {asset.GUID}, {pptr.FileIndex}, {asset.PathID}, {asset.IsValid}");
			}
		}
		static void UnknownFileType(object file, string filepath, string exportPath)
		{
			Console.WriteLine($"Unknown file {filepath}({file.GetType().Name })");
			Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
			using (var sw = new StreamWriter($"{exportPath}.err.txt"))
			{
				sw.WriteLine($"Can't dump file {file.GetType().FullName}");
			}
		}
		public static List<string> AllFilesInFolder(string folder)
		{
			var result = new List<string>();

			foreach (string f in Directory.GetFiles(folder))
			{
				result.Add(f);
			}

			foreach (string d in Directory.GetDirectories(folder))
			{
				result.AddRange(AllFilesInFolder(d));
			}
			return result;
		}
		static void WriteFileInfo(SerializedFile container, StreamWriter sw) {
			sw.WriteLine($"  File: {container.Name}");
			sw.WriteLine($"	File.Collection: {container.Collection}");
			sw.WriteLine($"	File.Platform: {container.Platform}");
			sw.WriteLine($"	File.Version: {container.Version}");
			foreach (var dep in container.Dependencies)
			{
				sw.WriteLine($"	File.Dependency: {dep}");
				sw.WriteLine($"	  Dependency.AssetPath: {dep.AssetPath}");
				sw.WriteLine($"	  Dependency.FilePath: {dep.FilePath}");
				sw.WriteLine($"	  Dependency.FilePathOrigin: {dep.FilePathOrigin}");
			}
			sw.WriteLine($"	File.Metadata: {container.Metadata.Entries.Count}");
			var factory = new AssetFactory();
			foreach (var entry in container.Metadata.Entries)
			{
				var info = entry.Value;
				AssetInfo assetInfo = new AssetInfo(container, info.PathID, info.ClassID);
				Object asset = factory.CreateAsset(assetInfo);
				if (asset == null)
				{
					sw.WriteLine($"	  Unimplemented Asset: {info.ClassID}, {info.ScriptID}, {info.TypeID}, {info.PathID}, {info.IsStripped}");
				}
			}
		}
		public static string HashToString(Hash128 hash)
		{
			var data = BitConverter.GetBytes(hash.Data0)
				.Concat(BitConverter.GetBytes(hash.Data1))
				.Concat(BitConverter.GetBytes(hash.Data2))
				.Concat(BitConverter.GetBytes(hash.Data3))
				.ToArray();
			return BitConverter.ToString(data).Replace("-", "");
		}
		public static void DumpBuildSettings(BuildSettings buildSettings, StreamWriter sw)
		{
			sw.WriteLine("BuildSettings");
			sw.WriteLine($"  Version: {buildSettings.Version}");
			sw.WriteLine("  Scenes");
			for(int i = 0; i < buildSettings.Scenes.Count; i++)
			{
				var scene = buildSettings.Scenes[i];
				sw.WriteLine($"	{i}: {scene}");
			}
			sw.WriteLine("  PreloadedPlugins");
			for (int i = 0; i < buildSettings.PreloadedPlugins.Count; i++)
			{
				var preloadedPlugin = buildSettings.PreloadedPlugins[i];
				sw.WriteLine($"	{i}: {preloadedPlugin}");
			}
			sw.WriteLine("  BuildTags");
			for (int i = 0; i < buildSettings.BuildTags.Count; i++)
			{
				var buildTag = buildSettings.BuildTags[i];
				sw.WriteLine($"	{i}: {buildTag}");
			}
			sw.WriteLine("  RuntimeClassHashes");
			foreach(var kv in buildSettings.RuntimeClassHashes.OrderBy(kv => kv.Key)) {
				sw.WriteLine($"	{kv.Key}: {HashToString(kv.Value)}");
			}
			sw.WriteLine("  ScriptHashes");
			foreach (var kv in buildSettings.ScriptHashes)
			{
				sw.WriteLine($"	{HashToString(kv.Key)}: {HashToString(kv.Value)}");
			}
		}
		static void DumpFile(string filepath, string exportPath)
		{
			var filename = Path.GetFileName(filepath);
			try
			{
				var fileCollection = new FileCollection();

				var scheme = FileCollection.LoadScheme(filepath, Path.GetFileName(filepath));

				if (scheme is SerializedFileScheme serializedFileScheme)
				{
					var serializedFile = serializedFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
					DumpFileInfo(serializedFile, Path.Combine(exportPath, filename));
				}
				if (scheme is BundleFileScheme bundleFileScheme)
				{
					var bundleFile = bundleFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
					DumpFileListInfo(bundleFile, Path.Combine(exportPath, filename));
				}
				if (scheme is ArchiveFileScheme archiveFileScheme)
				{
					var archiveFile = archiveFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
					DumpFileListInfo(archiveFile, Path.Combine(exportPath, filename));
				}
				if (scheme is WebFileScheme webFileScheme)
				{
					var webfile = webFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
					DumpFileListInfo(webfile, Path.Combine(exportPath, filename));
				}
				if (scheme is ResourceFileScheme resourceFileScheme)
				{
					var resourceFile = resourceFileScheme.ReadFile();
					UnknownFileType(resourceFile, filepath, Path.Combine(exportPath, filename));
				}
			} catch(Exception ex)
			{
				var errMessage = $"Error dumping file {filepath}\n{ex.ToString()}";
				Logger.Log(LogType.Error, LogCategory.General, errMessage);
				File.WriteAllText($"{exportPath}/{filename}.err.txt", errMessage);
			}
		}
		public static void DumpAllFileInfo(string gameDir, string exportPath)
		{
			Util.PrepareExportDirectory(exportPath);
			foreach(var file in AllFilesInFolder(gameDir))
			{
				var ext = Path.GetExtension(file);
				if (ext != "" && ext != ".assets" && ext != ".unity3d") continue;
				var relPath = Util.GetRelativePath(file, gameDir);
				relPath = Path.GetDirectoryName(relPath);
				if (Directory.Exists(file))
				{

				}
				DumpFile(file, Path.Combine(exportPath, relPath));
			}
		}
	}
}
