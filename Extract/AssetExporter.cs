using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using uTinyRipper;
using uTinyRipper.Assembly;
using uTinyRipper.AssetExporters;
using uTinyRipper.BundleFiles;
using uTinyRipper.Classes;
using uTinyRipperGUI.Exporters;
using DateTime = System.DateTime;
using Object = uTinyRipper.Classes.Object;

namespace Extract
{
    class AssetExporter
    {
        string GameDir;
        string AssetPath;
        string ExportPath;
        FileCollection fileCollection;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        Func<Object, bool> Selector = NoScriptSelector;
        public static bool AllSelector(Object obj)
        {
            return true;
        }
        public static bool NoScriptSelector(Object obj)
        {
            if (obj is MonoScript) return false;
            return true;
        }
        private AssetExporter(string gameDir, string assetPath, string exportPath, Func<Object, bool> selector = null)
        {
            GameDir = gameDir;
            AssetPath = assetPath;
            ExportPath = exportPath;
            Util.PrepareExportDirectory(ExportPath);
            if (selector != null) Selector = selector;
            fileCollection = new FileCollection(new FileCollection.Parameters()
            {
                RequestAssemblyCallback = RequestAssembly,
                RequestDependencyCallback = RequestDepency,
                RequestResourceCallback = RequestResource
            });
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var engineExporter = new EngineAssetExporter();
            var textureExporter = new TextureAssetExporter();
            fileCollection.Exporter.OverrideExporter(ClassIDType.Material, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Mesh, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Shader, new DummyShaderExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.Font, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Texture2D, textureExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Cubemap, textureExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Sprite, engineExporter); //engine or texture exporter?

            fileCollection.Exporter.EventExportProgressUpdated += ExportProgressUpdated;

        }
        void ExportProgressUpdated(int number, int total)
        {
            UpdateTitle($"Exported {number / (float)total * 100:0.#} - {number} of {total}");
        }

        HashSet<string> missingAssemblies = new HashSet<string>();
        private void RequestAssembly(string asm)
        {
            if (missingAssemblies.Contains(asm)) return;
            var assemblyPath = $"{GameDir}/Managed/{asm}.dll";
            if (File.Exists(assemblyPath))
            {
                Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Loading assembly {asm}");
                fileCollection.AssemblyManager.Load(assemblyPath);
            } else
            {
                if (asm == "Assembly-CSharp") throw new Exception("Can't find Assembly-CSharp");
                Logger.Instance.Log(LogType.Warning, LogCategory.Debug, $"Can't find assembly {asm}");
                missingAssemblies.Add(asm);
            }
        }
        Dictionary<string, string> DependencyLookup = new Dictionary<string, string>();
        HashSet<string> SeenDependencies = new HashSet<string>();
        private void RequestDepency(string dep)
        {
            Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Requested Dependency {dep}");
            var searchPaths = new string[]
            {
                "",
                "Resources",
                "StreamingAssets\\Bundles"
            };
            if (DependencyLookup.ContainsKey(dep))
            {
                fileCollection.Load(DependencyLookup[dep]);
                Logger.Log(LogType.Debug, LogCategory.Debug, $"Loading cached dependency {DependencyLookup[dep]}");
                return;
            }
            //TODO: why is there spaces?
            if(dep == "unity builtin extra")
            {
                fileCollection.Load($"{GameDir}\\Resources\\unity_builtin_extra");
                Logger.Log(LogType.Debug, LogCategory.Debug, $"Loaded dependency unity builtin extra");
                return;
            }
            foreach (var path in searchPaths)
            {
                var depPath = $@"{GameDir}\{path}\{dep}";
                if (File.Exists(depPath))
                {

                    DependencyLookup[dep] = depPath;
                    fileCollection.Load(depPath);
                    Logger.Log(LogType.Debug, LogCategory.Debug, $"Loaded dependency {depPath}");
                    return;
                }
            }
            if (File.Exists($"{AssetPath}.manifest"))
            {
                var manifestDependencies = Util.GetManifestDependencies($"{AssetPath}.manifest");
                foreach(var manifestDependency in manifestDependencies)
                {
                    if (SeenDependencies.Contains(manifestDependency)) continue;
                    SeenDependencies.Add(manifestDependency);
                    using (var bundleFile = BundleFile.Load($"{GameDir}\\StreamingAssets\\Bundles\\{manifestDependency}"))
                    {
                        foreach (var entry in bundleFile.Metadata.Entries)
                        {
                            if (entry.EntryType == FileEntryType.Serialized)
                            {
                                DependencyLookup[entry.Name.ToLower()] = entry.FilePath;
                            }
                        }
                    }
                }
                if (DependencyLookup.ContainsKey(dep))
                {
                    fileCollection.Load(DependencyLookup[dep]);
                    Logger.Log(LogType.Debug, LogCategory.Debug, $"Loaded bundle {DependencyLookup[dep]}");
                    return;
                }
            }
            throw new Exception($"Couldn't find dependency {dep}");
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
        private void Export(string assetPath)
        {
            fileCollection.Load(AssetPath);
            var file = Util.FindFile(fileCollection, assetPath);
            fileCollection.Exporter.Export(ExportPath, fileCollection, file.FetchAssets().Where(Selector));
        }
        private void ExportMultiple(IEnumerable<string> assetPaths)
        {
            fileCollection.Load(assetPaths.ToList());
            foreach (var assetPath in assetPaths)
            {
                var file = Util.FindFile(fileCollection, assetPath);
                fileCollection.Exporter.Export($"{ExportPath}/{Path.GetFileName(assetPath)}", fileCollection, file.FetchAssets().Where(Selector));
            }
        }
        public static void Export(string gameDir, string assetPath, string exportPath, Func<Object, bool> selector = null)
        {
            Util.PrepareExportDirectory(exportPath);
            new AssetExporter(gameDir, assetPath, exportPath, selector).Export(assetPath);
        }
        public static void ExportMultiple(string gameDir, IEnumerable<string> assetPaths, string exportPath)
        {
            Util.PrepareExportDirectory(exportPath);
            new AssetExporter(gameDir, "", exportPath).ExportMultiple(assetPaths);
        }
        static DateTime lastUpdate = DateTime.Now;
        public static void UpdateTitle(string text, params string[] arguments)
        {
            var now = DateTime.Now;
            var delta = now - lastUpdate;
            if (delta.TotalMilliseconds > 500)
            {
                Console.Title = string.Format(text, arguments);
                lastUpdate = now;
            }
            lastUpdate = now;
        }
        internal static void ExportBundles(string gameDir, IEnumerable<string> bundles, string exportDir, bool randomizeGUID = true)
        {
            new AssetExporter(gameDir, "", exportDir).DoExportBundles(bundles, randomizeGUID);
        }
        private void DoExportBundles(IEnumerable<string> bundles, bool randomizeGUID)
        {
            Config.IsExportDependencies = true;
            Config.IsGenerateGUIDByContent = true;
            var fileCount = bundles.Count();
            var sessionGUID = Guid.NewGuid();
            var i = 0;
            foreach(var filePath in bundles)
            {
                var assetName = Path.GetFileName(filePath);
                if (assetName.StartsWith("resource_"))
                {
                    assetName = Util.GetManifestAssets($"{filePath}.manifest")
                        .First();
                    assetName = Path.GetFileNameWithoutExtension(assetName);
                }
                Logger.Log(LogType.Info, LogCategory.Export, $"Exporting bundle {assetName}");
                UpdateTitle($"Exporting {i++ / (float)fileCount * 100:0.#}% - {AssetPath}");
                AssetPath = filePath;
                fileCollection.Load(filePath);
                Util.FixShaderBundle(fileCollection);
                var file = Util.FindFile(fileCollection, filePath);
                var assets = file.FetchAssets().Where(Selector);
                if (Path.GetFileName(filePath) != "common_assets" && Path.GetFileName(filePath) != "shaders") {
                    if (randomizeGUID)
                    {
                        Util.RandomizeAssetGuid(assets);
                    } else
                    {
                        foreach(var asset in assets)
                        {
                            var name = Util.GetName(asset);
                            if (string.IsNullOrEmpty(name))
                            {
                                name = Guid.NewGuid().ToString();
                                var namelessAssets = new HashSet<ClassIDType>()
                                {
                                    ClassIDType.GameObject,
                                    ClassIDType.Transform,
                                    ClassIDType.Animator,
                                    //ClassIDType.MonoBehaviour,
                                    ClassIDType.SkinnedMeshRenderer,
                                };
                                if (!namelessAssets.Contains(asset.ClassID))
                                {
                                    Logger.Log(LogType.Warning, LogCategory.Export, $"Asset type {asset.ClassID} has no name");
                                }
                            }
                            using (var md5 = System.Security.Cryptography.MD5.Create())
                            {
                                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(name + sessionGUID));
                                Util.SetGUID(asset, new Guid(hash));
                            }
                           
                        }
                    }
                } 
                fileCollection.Exporter.Export($"{ExportPath}/{assetName}", fileCollection, assets);
                Directory.CreateDirectory($"{ExportPath}/{assetName}"); //TODO: Fis
                if(Directory.Exists($"{ExportPath}/{assetName}/Assets/Shader")) Directory.Delete($"{ExportPath}/{assetName}/Assets/Shader", true);
                if(Directory.Exists($"{ExportPath}/{assetName}/Assets/Scripts"))  Directory.Delete($"{ExportPath}/{assetName}/Assets/Scripts", true);
                File.Copy($"{filePath}.manifest", $"{ExportPath}/{assetName}/{Path.GetFileName(filePath)}.manifest", true);
                fileCollection.UnloadAll();

            }
        }
    }
}
