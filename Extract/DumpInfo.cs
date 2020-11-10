using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Misc;
using uTinyRipper.SerializedFiles;
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
				DumpObjectInfo(container, sw);
				if (container.Name == "globalgamemanagers")
				{
					BuildSettings buildSettings = (BuildSettings)container.FetchAssets().FirstOrDefault(asset => asset is BuildSettings);
					if (buildSettings != null)
					{
						sw.WriteLine("");
						DumpBuildSettings(buildSettings, sw);
					}
					MonoManager monoManager = (MonoManager)container.FetchAssets().FirstOrDefault(asset => asset is MonoManager);
					if (monoManager != null)
					{
						sw.WriteLine("");
						DumpMonoManager(monoManager, sw);
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
				if (fileList is BundleFile bf)
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
					sw.WriteLine($"ResourceFile: Name {resourceFile.Name}");
				}
				sw.WriteLine($"");
				sw.WriteLine($"SerializedFile count {fileList.SerializedFiles.Count}");
				foreach (var serializedFile in fileList.SerializedFiles)
				{
					sw.WriteLine("");
					WriteFileInfo(serializedFile, sw);
					sw.WriteLine("");
					DumpObjectInfo(serializedFile, sw);
				}
			}
		}
		static void DumpBundleFileInfo(BundleFile bundleFile, StreamWriter sw)
		{
			sw.WriteLine("BundleFile");
			var metadata = bundleFile.Metadata;
			var header = bundleFile.Header;
			sw.WriteLine("  TODO");
		}
		static void DumpObjectInfo(SerializedFile file, StreamWriter sw)
		{
			sw.WriteLine("{0,-40}, {1,30}, {2,-32}, {3}, {4}, {5}, {6}",
				"Name", "ClassID", "GUID", "FileIndex", "PathID", "IsValid", "Extra");
			var lookup = file.FetchAssets().ToDictionary(a => a.PathID, a => a);
			foreach (var asset in file.FetchAssets())
			{
				var name = Util.GetName(asset);
				var pptr = asset.File.CreatePPtr(asset);
				var extra = "";
				if (asset is MonoScript ms)
				{
					string scriptName = $"[{ms.AssemblyName}]";
					if (!string.IsNullOrEmpty(ms.Namespace)) scriptName += $"{ms.Namespace}.";
					scriptName += $"{ms.ClassName}:{HashToString(ms.PropertiesHash)}";
					extra = scriptName;
				}
				sw.WriteLine($"{name,-40}, {asset.ClassID,30}, {asset.GUID}, {pptr.FileIndex,9}, {asset.PathID,6}, {extra}");
			}
			sw.WriteLine();
			sw.WriteLine("Cannot parse");
			sw.WriteLine("{0,-6}, {1,-40}, {2,-6}, {3,-15}, {4,-8}, {5,-11}, {6,-9}, {7,-8}",
	"FileID", "ClassID", "TypeID", "ScriptTypeIndex", "Stripped", "IsDestroyed", "ByteStart", "ByteSize");
			foreach (var info in file.Metadata.Object)
			{
				if (lookup.ContainsKey(info.FileID)) continue;
				sw.WriteLine($"{info.FileID,-6}, {info.ClassID,-40}, {info.TypeID, -6}, {info.ScriptTypeIndex,-15}, {info.Stripped,-8}, {info.IsDestroyed,-11}, {info.ByteStart,-9}, {info.ByteSize,-8}");
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
		static void WriteFileInfo(SerializedFile container, StreamWriter sw)
		{
			sw.WriteLine($"  File: {container.Name}");
			sw.WriteLine($"	File.Collection: {container.Collection}");
			sw.WriteLine($"	File.Platform: {container.Platform}");
			sw.WriteLine($"	File.Version: {container.Version}");
			foreach (var dep in container.Dependencies)
			{
				sw.WriteLine($"	File.Dependency: {dep}");
				sw.WriteLine($"	  Dependency.AssetPath: {dep.AssetPath}");
			}
			if (container.Metadata != null)
			{
				//TODO container.Metadata.Hierarchy
				/*var SerializeTypeTrees = container.Metadata.Hierarchy.SerializeTypeTrees;
				var Types = container.Metadata.Hierarchy.Types;
				var Version = container.Metadata.Hierarchy.Version;
				var Platform = container.Metadata.Hierarchy.Platform;
				var Unknown = container.Metadata.Hierarchy.Unknown;
				sw.WriteLine($"	File.Metadata.Hierarchy:");
				sw.WriteLine($"		Hierarchy.Version: {Version}");
				sw.WriteLine($"		Hierarchy.Platform: {Platform}");
				sw.WriteLine($"		Hierarchy.SerializeTypeTrees: {SerializeTypeTrees}");
				sw.WriteLine($"		Hierarchy.Unknown: {Unknown}");
				sw.WriteLine($"		Hierarchy.Types: {Types.Length}");
				if (Types.Length > 0)
				{
					sw.WriteLine("			{0,-18}, {1}, {2}, {3,-32}, {4,-32}, {5}",
						"ClassId", "IsStrippedType", "ScriptID", "ScriptHash", "TypeHash", "NodeCount");
				}
				foreach (var type in Types)
				{
					var ClassID = Util.GetMember<ClassIDType>(type, "ClassID");
					var IsStrippedType = Util.GetMember<bool>(type, "IsStrippedType");
					var ScriptID = Util.GetMember<short>(type, "ScriptID");
					var Tree = Util.GetMember<object>(type, "Tree");
					var ScriptHash = Util.GetMember<Hash128>(type, "ScriptHash");
					var TypeHash = Util.GetMember<Hash128>(type, "TypeHash");
					var nodeCount = Tree == null ? "Null" : Util.GetMember<IList>(Tree, "Nodes").Count.ToString();
					sw.WriteLine("			{0,-18}, {1,14}, {2,8}, {3}, {4}, {5}",
						ClassID.ToString(), IsStrippedType, ScriptID, HashToString(ScriptHash), HashToString(TypeHash), nodeCount);
				}*/
			}
			else
			{
				sw.WriteLine($"	File.Metadata.Hierarchy: Null");
			}
			//TODO
			/*sw.WriteLine($"	File.Metadata.Entries: {container.Metadata.Entries.Length}");

			var factory = new AssetFactory();
			foreach (var entry in container.Metadata.Entries)
			{
				AssetInfo assetInfo = new AssetInfo(container, entry.PathID, entry.ClassID);
				Object asset = factory.CreateAsset(assetInfo);
				if (asset == null)
				{
					sw.WriteLine($"	  Unimplemented Asset: {entry.ClassID}, {entry.ScriptID}, {entry.TypeID}, {entry.PathID}, {entry.IsStripped}");
				}
			}*/
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
			sw.WriteLine($"  Scenes {buildSettings.Scenes.Length}");
			for (int i = 0; i < buildSettings.Scenes.Length; i++)
			{
				var scene = buildSettings.Scenes[i];
				sw.WriteLine($"	{i}: {scene}");
			}
			sw.WriteLine($"  PreloadedPlugins {buildSettings.PreloadedPlugins}");
			for (int i = 0; i < buildSettings.PreloadedPlugins.Length; i++)
			{
				var preloadedPlugin = buildSettings.PreloadedPlugins[i];
				sw.WriteLine($"	{i}: {preloadedPlugin}");
			}
			sw.WriteLine($"  BuildTags {buildSettings.BuildTags.Length}");
			for (int i = 0; i < buildSettings.BuildTags.Length; i++)
			{
				var buildTag = buildSettings.BuildTags[i];
				sw.WriteLine($"	{i}: {buildTag}");
			}
			sw.WriteLine($"  RuntimeClassHashes {buildSettings.RuntimeClassHashes.Count}");
			foreach (var kv in buildSettings.RuntimeClassHashes.OrderBy(kv => kv.Key))
			{
				sw.WriteLine($"	{kv.Key}: {HashToString(kv.Value)}");
			}
			sw.WriteLine($"  ScriptHashes {buildSettings.ScriptHashes.Count}");
			foreach (var kv in buildSettings.ScriptHashes)
			{
				sw.WriteLine($"	{HashToString(kv.Key)}: {HashToString(kv.Value)}");
			}
		}
		public static void DumpMonoManager(MonoManager monoManager, StreamWriter sw)
		{
			sw.WriteLine("MonoManager");
			sw.WriteLine($"  HasCompileErrors {monoManager.HasCompileErrors}");
			sw.WriteLine($"  EngineDllModDate {monoManager.EngineDllModDate}");
			sw.WriteLine($"  CustomDlls {monoManager.CustomDlls?.Length}");
			foreach (var dll in monoManager.CustomDlls ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyIdentifiers {monoManager.AssemblyIdentifiers?.Length}");
			foreach (var dll in monoManager.AssemblyIdentifiers ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyNames {monoManager.AssemblyNames?.Length}");
			foreach (var dll in monoManager.AssemblyNames ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyTypes {monoManager.AssemblyTypes?.Length}");
			foreach (var dll in monoManager.AssemblyTypes ?? new int[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  Scripts {monoManager.Scripts.Length}");
			foreach (var dll in monoManager.Scripts ?? new PPtr<MonoScript>[] { })
			{
				sw.WriteLine($"    {dll}");
			}
		}
		static void DumpFile(string filepath, string exportPath)
		{
			var filename = Path.GetFileName(filepath);
			try
			{
				var file = Util.LoadFile(filepath);
				if (file is SerializedFile serializedFile)
				{
					DumpFileInfo(serializedFile, Path.Combine(exportPath, filename));
				}
				if (file is BundleFile bundleFile)
				{
					DumpFileListInfo(bundleFile, Path.Combine(exportPath, filename));
				}
				if (file is ArchiveFile archiveFile)
				{
					DumpFileListInfo(archiveFile, Path.Combine(exportPath, filename));
				}
				if (file is WebFile webfile)
				{
					DumpFileListInfo(webfile, Path.Combine(exportPath, filename));
				}
				if (file is ResourceFile resourceFile)
				{
					UnknownFileType(resourceFile, filepath, Path.Combine(exportPath, filename));
				}
			}
			catch (Exception ex)
			{
				var errMessage = $"Error dumping file {filepath}\n{ex.ToString()}";
				Logger.Log(LogType.Error, LogCategory.General, errMessage);
				Directory.CreateDirectory(exportPath);
				File.WriteAllText($"{exportPath}/{filename}.err.txt", errMessage);
			}
		}
		public static void DumpAllFileInfo(string gameDir, string exportPath)
		{
			Util.PrepareExportDirectory(exportPath);
			foreach (var file in AllFilesInFolder(gameDir))
			{
				var ext = Path.GetExtension(file);
				if (ext != "" && ext != ".assets" && ext != ".unity3d" && ext != ".bundle") continue;
				var relPath = Util.GetRelativePath(file, gameDir);
				relPath = Path.GetDirectoryName(relPath);
				if (Directory.Exists(file))
				{

				}
				DumpFile(file, Path.Combine(exportPath, relPath));
			}
		}
		public static void DumpTypeTree(string filePath, string exportPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
			var file = Util.LoadFile(filePath);
			var seralizedFiles = new List<SerializedFile>();
			if (file is SerializedFile sf)
			{
				seralizedFiles.Add(sf);
			}
			else if (file is BundleFile bundleFile)
			{
				seralizedFiles.AddRange(bundleFile.SerializedFiles);
			}
			else
			{
				throw new Exception();
			}
			using (var sw = new StreamWriter(exportPath))
			{
				foreach (var serializedFile in seralizedFiles)
				{
					DumpTypeInfo(serializedFile, sw);
				}
			}
		}
		static bool CompareHash(Hash128 hash1, Hash128 hash2)
		{
			return hash1.Data0 == hash2.Data0 &&
			hash1.Data1 == hash2.Data1 &&
			hash1.Data2 == hash2.Data2 &&
			hash1.Data3 == hash2.Data3;
		}
		internal static void DumpTypeInfo(SerializedFile serializedFile, StreamWriter sw)
		{
			foreach(var asset in serializedFile.FetchAssets().Where(asset => asset is MonoScript))
			{
				var monoScript = asset as MonoScript;
				sw.WriteLine($"\t[{monoScript.AssemblyName}]{monoScript.Namespace}.{monoScript.ClassName} - {HashToString(monoScript.PropertiesHash)}");

			}
			sw.WriteLine($"SerializedFile");
			sw.WriteLine($"Name {serializedFile.Name}");
			sw.WriteLine($"NameOrigin {serializedFile.NameOrigin}");
			sw.WriteLine($"Platform {serializedFile.Platform}");
			sw.WriteLine($"Version {serializedFile.Version}");
			//TODO
			/*sw.WriteLine($"Preloads:");
			foreach (var ptr in serializedFile.Metadata.Preloads)
			{
				sw.WriteLine($"\t{ptr}");
			}
			var hierarchy = serializedFile.Metadata.Hierarchy;
			sw.WriteLine($"TypeTree Version {hierarchy.Version}");
			sw.WriteLine($"TypeTree Platform {hierarchy.Platform}");
			var SerializeTypeTrees = Util.GetMember<bool>(hierarchy, "SerializeTypeTrees");
			sw.WriteLine($"TypeTree SerializeTypeTrees {SerializeTypeTrees}");
			var Unknown = Util.GetMember<int>(hierarchy, "Unknown");
			sw.WriteLine($"TypeTree Unknown {Unknown}");
			sw.WriteLine($"");
			var types = Util.GetMember<IReadOnlyList<object>>(hierarchy, "Types");
			foreach (var type in types)
			{
				var ClassID = Util.GetMember< ClassIDType > (type, "ClassID");
				var ScriptID = Util.GetMember<short >(type, "ScriptID");
				var IsStrippedType = Util.GetMember<bool>(type, "IsStrippedType");
				var Tree = Util.GetMember<object>(type, "Tree");
				var ScriptHash = Util.GetMember<Hash128>(type, "ScriptHash");
				var TypeHash = Util.GetMember<Hash128>(type, "TypeHash");

				var monoScript = serializedFile.FetchAssets().FirstOrDefault(asset => asset is MonoScript ms && CompareHash(ms.PropertiesHash, TypeHash)) as MonoScript;
				string scriptType = monoScript == null ? "\tNo Script" : $"\tMonoScript is [{monoScript.AssemblyName}]{monoScript.Namespace}.{monoScript.ClassName}";
				sw.WriteLine(scriptType);
				sw.WriteLine($"\tType: ClassID {ClassID}, ScriptID {ScriptID}, IsStrippedType {IsStrippedType}, ScriptHash {HashToString(ScriptHash)}, TypeHash {HashToString(TypeHash)}");
				var Dump = Util.GetMember<string>(Tree, "Dump");
				sw.WriteLine($"\t{Dump}");
				sw.WriteLine($"");
			}*/
			sw.WriteLine($"");
		}
	}
}
