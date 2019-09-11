using Extract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.ArchiveFiles;
using uTinyRipper.Assembly;
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
				var extra = "";
				if (asset is MonoScript ms)
				{
					string scriptName = $"[{ms.AssemblyName}]";
					if (!string.IsNullOrEmpty(ms.Namespace)) scriptName += $"{ms.Namespace}.";
					scriptName += $"{ms.ClassName}:{HashToString(ms.PropertiesHash)}";
					extra = scriptName;
				}
				sw.WriteLine($"{name}, {asset.ClassID}, {asset.GUID}, {pptr.FileIndex}, {asset.PathID}, {asset.IsValid},{extra}");
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
				sw.WriteLine($"	  Dependency.FilePath: {dep.FilePath}");
				sw.WriteLine($"	  Dependency.FilePathOrigin: {dep.FilePathOrigin}");
			}
			if (container.Metadata.Hierarchy != null)
			{
				var SerializeTypeTrees = Util.GetMember<bool>(container.Metadata.Hierarchy, "SerializeTypeTrees");
				var Types = Util.GetMember<IReadOnlyList<object>>(container.Metadata.Hierarchy, "Types");
				var Name = Util.GetMember<string>(container.Metadata.Hierarchy, "Name");
				sw.WriteLine($"	File.Metadata.Hierarchy:");
				sw.WriteLine($"		Hierarchy.Name: {Name}");
				sw.WriteLine($"		Hierarchy.SerializeTypeTrees: {SerializeTypeTrees}");
				sw.WriteLine($"		Hierarchy.Types: {Types.Count}");
				if (Types.Count > 0)
				{
					sw.WriteLine($"			ClassID, IsStrippedType, ScriptID, ScriptHash, TypeHash, NodeCount");
				}
				foreach (var type in Types)
				{
					var ClassID = Util.GetMember<ClassIDType>(type, "ClassID");
					var IsStrippedType = Util.GetMember<bool>(type, "IsStrippedType");
					var ScriptID = Util.GetMember<short>(type, "ScriptID");
					var Tree = Util.GetMember<object>(type, "Tree");
					var ScriptHash = Util.GetMember<Hash128>(type, "ScriptHash");
					var TypeHash = Util.GetMember<Hash128>(type, "TypeHash");
					var nodeCount = Tree == null ? "Null" : Util.GetMember<IReadOnlyList<object>>(Tree, "Nodes").Count.ToString();
					sw.WriteLine($"			{ClassID}, {IsStrippedType}, {ScriptID}, {HashToString(ScriptHash)}, {HashToString(TypeHash)}, {nodeCount}");
				}
			}
			else
			{
				sw.WriteLine($"	File.Metadata.Hierarchy: Null");
			}
			sw.WriteLine($"	File.Metadata.Entries: {container.Metadata.Entries.Count}");

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
			sw.WriteLine($"  Scenes {buildSettings.Scenes.Count}");
			for (int i = 0; i < buildSettings.Scenes.Count; i++)
			{
				var scene = buildSettings.Scenes[i];
				sw.WriteLine($"	{i}: {scene}");
			}
			sw.WriteLine($"  PreloadedPlugins {buildSettings.PreloadedPlugins}");
			for (int i = 0; i < buildSettings.PreloadedPlugins.Count; i++)
			{
				var preloadedPlugin = buildSettings.PreloadedPlugins[i];
				sw.WriteLine($"	{i}: {preloadedPlugin}");
			}
			sw.WriteLine($"  BuildTags {buildSettings.BuildTags.Count}");
			for (int i = 0; i < buildSettings.BuildTags.Count; i++)
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
			sw.WriteLine($"  CustomDlls {monoManager.CustomDlls?.Count}");
			foreach (var dll in monoManager.CustomDlls ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyIdentifiers {monoManager.AssemblyIdentifiers?.Count}");
			foreach (var dll in monoManager.AssemblyIdentifiers ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyNames {monoManager.AssemblyNames?.Count}");
			foreach (var dll in monoManager.AssemblyNames ?? new string[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  AssemblyTypes {monoManager.AssemblyTypes?.Count}");
			foreach (var dll in monoManager.AssemblyTypes ?? new int[] { })
			{
				sw.WriteLine($"    {dll}");
			}
			sw.WriteLine($"  Scripts {monoManager.Scripts.Count}");
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
			}
			catch (Exception ex)
			{
				var errMessage = $"Error dumping file {filepath}\n{ex.ToString()}";
				Logger.Log(LogType.Error, LogCategory.General, errMessage);
				File.WriteAllText($"{exportPath}/{filename}.err.txt", errMessage);
			}
		}
		public static void DumpAllFileInfo(string gameDir, string exportPath)
		{
			Util.PrepareExportDirectory(exportPath);
			foreach (var file in AllFilesInFolder(gameDir))
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
		public static void DumpSerializedTypes(string filePath, string managedPath, string exportPath)
		{
			Util.PrepareExportDirectory(exportPath);
			Directory.CreateDirectory(exportPath);
			var fileCollection = new FileCollection();
			fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
			foreach (var assembly in Directory.GetFiles(managedPath, "*.dll", SearchOption.TopDirectoryOnly))
			{
				fileCollection.AssemblyManager.Load(assembly);
			}
			var scheme = FileCollection.LoadScheme(filePath, Path.GetFileName(filePath));
			var seralizedFiles = new List<SerializedFile>();
			if (scheme is SerializedFileScheme serializedFileScheme)
			{
				var serializedFile = serializedFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
				seralizedFiles.Add(serializedFile);

			}
			else if (scheme is BundleFileScheme bundleFileScheme)
			{
				var bundleFile = bundleFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
				seralizedFiles.AddRange(bundleFile.SerializedFiles);
			}
			else
			{
				throw new Exception();
			}

			using (var sw = new StreamWriter($"{exportPath}/dump.txt"))
			{
				foreach (var serializedFile in seralizedFiles)
				{
					var assets = serializedFile.FetchAssets();
					foreach (var asset in assets)
					{
						if (asset is MonoScript monoscript && monoscript.AssemblyName == "Assembly-CSharp")
						{
							SerializableType behaviourType = monoscript.GetBehaviourType();
							if (behaviourType != null)
							{
								var Structure = behaviourType.CreateBehaviourStructure();
								sw.WriteLine($"[{monoscript.AssemblyName}]{monoscript.Namespace}.{monoscript.Name}");
								DumpSerializedTypes(Structure, sw, 0);
								sw.WriteLine($"");
							}
							else
							{
								sw.WriteLine($"[{monoscript.AssemblyName}]{monoscript.Namespace}.{monoscript.Name} - No Behaviour Type");
								sw.WriteLine($"");
							}
						}
					}
				}
			}
		}
		public static void DumpSerializedTypes(SerializableStructure structure, StreamWriter sw, int indent)
		{
			string spaces = new string(' ', indent * 2);
			if (indent > 10)
			{
				sw.WriteLine($"{spaces}Max depth exceded");
			}
			sw.WriteLine($"{spaces}{structure.Type} : {structure.Base?.ToString() ?? "object"}");
			if(structure.Base != null && structure.Base.Type.Namespace != "UnityEngine")
			{
				DumpSerializedTypes(structure.Base, sw, indent + 1);
			}
			foreach (var field in structure.Fields)
			{
				spaces = new string(' ', (indent + 1) * 2);
				if (field.Type == PrimitiveType.Complex)
				{
					ISerializableStructure ComplexType = Util.GetMember<ISerializableStructure>(field, "ComplexType");
					if (ComplexType is SerializablePointer pptr)
					{
						sw.WriteLine($"{spaces}PPTR<{ComplexType}> {field.Name};");
					}
					else if(ComplexType is SerializableStructure fieldStructure)
					{
						sw.WriteLine($"{spaces}{fieldStructure} {field.Name};");
						DumpSerializedTypes(fieldStructure, sw, indent + 1);
					} else
					{
						sw.WriteLine($"{spaces}{ComplexType} {field.Name};");
					}
				}
				else
				{
					sw.WriteLine($"{spaces}{field.Type} {field.Name};");
				}
			}
		}
		public static void DumpTypeTree(string filePath, string exportPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
			var fileCollection = new FileCollection();
			var scheme = FileCollection.LoadScheme(filePath, Path.GetFileName(filePath));
			var seralizedFiles = new List<SerializedFile>();
			if (scheme is SerializedFileScheme serializedFileScheme)
			{
				var serializedFile = serializedFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
				seralizedFiles.Add(serializedFile);

			}
			else if (scheme is BundleFileScheme bundleFileScheme)
			{
				var bundleFile = bundleFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
				seralizedFiles.AddRange(bundleFile.SerializedFiles);
			}
			else
			{
				throw new Exception();
			}
			using (var sw = new StreamWriter(exportPath))
			{
				foreach (var file in seralizedFiles)
				{
					DumpTypeInfo(file, sw);
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
			sw.WriteLine($"Preloads:");
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
			}
			sw.WriteLine($"");
		}
	}
}
