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

namespace Extract
{
    public class DumpInfo
    {
        static void DumpFileInfo(ISerializedFile container, string gameStructureDir)
        {
            Console.WriteLine($"Dumping container {container.Name }");
            Directory.CreateDirectory(gameStructureDir);
            using (var sw = new StreamWriter(Path.Combine(gameStructureDir, $"{container.Name}.txt")))
            {
                WriteFileInfo(container, sw);
                sw.WriteLine("");
                sw.WriteLine("Name, ClassID, GUID, InstanceID, PathID, LocalIdentfierInFile, asset.IsValid");
                foreach (var asset in container.FetchAssets())
                {
                    var nameProp = asset.GetType().GetProperty("Name");
                     if(nameProp != null)
                    {
                        var name = nameProp.GetValue(asset);
                        sw.WriteLine($"{name}, {asset.ClassID}, {asset.GUID}, {asset.InstanceID}, {asset.PathID}, {asset.LocalIdentfierInFile}, {asset.IsValid}");
                    }
                    else
                    {
                        sw.WriteLine($"NamelessObject, {asset.ClassID}, {asset.GUID}, {asset.InstanceID}, {asset.PathID}, {asset.LocalIdentfierInFile}, {asset.IsValid}");
                    }
                }
            }
        }
        static void UnknownFileType(object file, string filepath, string exportPath)
        {
            string gameStructureDir = Path.Combine(exportPath, Path.GetFileName(filepath));
            Console.WriteLine($"Unknown file {file.GetType().Name }");
            Directory.CreateDirectory(gameStructureDir);
            using (var sw = new StreamWriter(Path.Combine(gameStructureDir, $"{file.GetType().Name}.unknown.txt")))
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
        static void WriteFileInfo(ISerializedFile container, StreamWriter sw) {
            sw.WriteLine($"  File: {container.Name}");
            sw.WriteLine($"    File.Collection: {container.Collection}");
            sw.WriteLine($"    File.Platform: {container.Platform}");
            sw.WriteLine($"    File.Version: {container.Version}");
            foreach (var dep in container.Dependencies)
            {
                sw.WriteLine($"    File.Dependency: {dep}");
                sw.WriteLine($"      Dependency.AssetPath: {dep.AssetPath}");
                sw.WriteLine($"      Dependency.FilePath: {dep.FilePath}");
                sw.WriteLine($"      Dependency.FilePathOrigin: {dep.FilePathOrigin}");
            }
        }
        static void DumpFileStructure(GameStructure gameStructure, string exportPath, string fileName)
        {
            using (var sw = new StreamWriter(Path.Combine(exportPath, $"{Path.GetFileName(fileName)}.txt")))
            {
                Console.WriteLine($"Dumping bundle {fileName }");

                sw.WriteLine($"AssetBundle: {fileName}");
                sw.WriteLine($"  Name: {gameStructure.Name}");
                //TODO: Fix
                //sw.WriteLine($"  Version: {gameStructure.Version}");
                sw.WriteLine($"  MixedStructure: {gameStructure.MixedStructure?.ToString() ?? "NULL"}");
                if (gameStructure.MixedStructure != null)
                {
                    foreach (var container in gameStructure.MixedStructure.Files)
                    {
                        sw.WriteLine($"    File: {container.Key} = {container.Value}");
                    }
                    foreach (var assembly in gameStructure.MixedStructure.Assemblies)
                    {
                        sw.WriteLine($"    Assembly: {assembly.Key} = {assembly.Value}");
                    }
                    foreach (var dataPath in gameStructure.MixedStructure.DataPathes)
                    {
                        sw.WriteLine($"    DataPath: {dataPath}");
                    }
                }
                sw.WriteLine($"  PlatformStructure: {gameStructure.PlatformStructure?.ToString() ?? "NULL"}");
                if (gameStructure.PlatformStructure != null)
                {
                    foreach (var container in gameStructure.PlatformStructure.Files)
                    {
                        sw.WriteLine($"    File: {container.Key} = {container.Value}");
                    }
                    foreach (var assembly in gameStructure.PlatformStructure.Assemblies)
                    {
                        sw.WriteLine($"    Assembly: {assembly.Key} = {assembly.Value}");
                    }
                    foreach (var dataPath in gameStructure.PlatformStructure.DataPathes)
                    {
                        sw.WriteLine($"    DataPath: {dataPath}");
                    }
                }
                sw.WriteLine($"  FileCollection: {gameStructure.FileCollection?.ToString() ?? "NULL"}");
                foreach (var container in gameStructure.FileCollection.Files)
                {
                    WriteFileInfo(container, sw);
                }
            }
        }
        public static void DumpFile(string fileName, string exportPath)
        {
            Console.WriteLine($"Loading {fileName}");
            try
            {
                var gameStructure = GameStructure.Load(new string[] { fileName });
                Directory.CreateDirectory(exportPath);
                DumpFileStructure(gameStructure, exportPath, fileName);
                foreach (var kv in gameStructure.MixedStructure.Files)
                {

                    var container = gameStructure.FileCollection.Files.FirstOrDefault(f => f is SerializedFile sf && sf.FilePath == kv.Value);
                    if (fileName.EndsWith("testbundle"))
                    {

                    }
                    if (fileName.EndsWith("resources"))
                    {

                    }
                    DumpFileInfo(container, Path.Combine(exportPath, gameStructure.Name));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static void DumpAllFileInfo(string gameDir, string exportPath)
        {
            var gameStructure = GameStructure.Load(new string[] {
                gameDir,
                Path.Combine(gameDir, "globalgamemanagers.assets"),
                Path.Combine(gameDir, "resources.assets")
            });
            Directory.CreateDirectory(exportPath);
            DumpFileStructure(gameStructure, exportPath, "GameStructure.txt");
            var mixedStructure = gameStructure.MixedStructure;
            if (mixedStructure != null)
            {
                foreach (var kv in gameStructure.MixedStructure.Files)
                {
                    var container = gameStructure.FileCollection.Files.FirstOrDefault(f => f is SerializedFile sf && sf.FilePath == kv.Value);
                    DumpFileInfo(container, Path.Combine(exportPath, $"MixedStructure_{gameStructure.Name}"));
                }
            }
            var platformStructure = gameStructure.PlatformStructure;
            if (platformStructure != null)
            {
                foreach (var kv in gameStructure.PlatformStructure.Files)
                {
                    var container = gameStructure.FileCollection.Files.FirstOrDefault(f => f is SerializedFile sf && sf.FilePath == kv.Value);
                    DumpFileInfo(container, Path.Combine(exportPath, $"PlatformStructure_{gameStructure.Name}"));
                }
            }
        }
        static void DumpFileFast(string filepath, string exportPath)
        {
            var fileCollection = new FileCollection();
            var filename = Path.GetFileName(filepath);
            var scheme = FileCollection.LoadScheme(filepath, Path.GetFileName(filepath));
            if (scheme is BundleFileScheme bundleFileScheme)
            {
                var bundleFile = bundleFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
                UnknownFileType(bundleFile, filepath, exportPath);
            }
            if (scheme is ArchiveFileScheme archiveFileScheme)
            {
                var archiveFile = archiveFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
                UnknownFileType(archiveFile, filepath, exportPath);
            }
            if (scheme is WebFileScheme webFileScheme)
            {
                var webfile = webFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
                UnknownFileType(webfile, filepath, exportPath);
            }
            if (scheme is SerializedFileScheme serializedFileScheme)
            {
                var serializedFile = serializedFileScheme.ReadFile(fileCollection, fileCollection.AssemblyManager);
                DumpFileInfo(serializedFile, Path.Combine(exportPath, filename));
            }
            if (scheme is ResourceFileScheme resourceFileScheme)
            {
                var resourceFile =  resourceFileScheme.ReadFile();
                UnknownFileType(resourceFile, filepath, exportPath);
            }
        }
        public static void DumpAllFileInfoFast(string gameDir, string exportPath)
        {
            Util.PrepareExportDirectory(exportPath);
            var files = Directory.GetFiles($@"{gameDir}")
                .Where(f => !f.EndsWith("resS"))
                .Where(f => Path.GetFileName(f).StartsWith("level") || Path.GetFileName(f).StartsWith("sharedassets"))
                .Concat(new string[]
                {
                    $@"{gameDir}\globalgamemanagers",
                    $@"{gameDir}\globalgamemanagers.assets",
                    $@"{gameDir}\resources.assets",
                });
            foreach (var fileName in files)
            {
                DumpFileFast(fileName, Path.Combine(exportPath, "Data"));
            }
            if (Directory.Exists($@"{gameDir}\StreamingAssets\Bundles"))
            {
                var resources = Directory.GetFiles($@"{gameDir}\StreamingAssets\Bundles")
                    .Where(f => Path.GetFileName(f).StartsWith("resource") && !f.EndsWith(".manifest"));
                foreach (var fileName in resources)
                {
                    DumpFileFast(fileName, $"{exportPath}\\Resources");
                }
                var scenes = Directory.GetFiles($@"{gameDir}\StreamingAssets\Bundles")
                    .Where(f => Path.GetFileName(f).StartsWith("resource") && !f.EndsWith(".manifest"));
                foreach (var fileName in scenes)
                {
                    DumpFileFast(fileName, $"{exportPath}\\StreamingScenes");
                }
            }
        }
    }
}
