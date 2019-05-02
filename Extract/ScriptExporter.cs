using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Assembly;
using uTinyRipper.Classes;
using uTinyRipper.Exporters.Scripts;
using uTinyRipper.SerializedFiles;
using Object = uTinyRipper.Classes.Object;

namespace Extract
{
    class ScriptExporter
    {
        string GameDir;
        FileCollection fileCollection;
        private string ExportPath;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        public ScriptExporter(string gameDir, string exportPath)
        {
            GameDir = gameDir;
            ExportPath = exportPath;
        }
        public static void ExportAll(string GameDir, string exportPath)
        {
            Util.PrepareExportDirectory(exportPath);
            var scriptExporter = new ScriptExporter(GameDir, exportPath);
            scriptExporter.DoExportAll();
        }
        public static void Export(string GameDir, string exportPath, Func<MonoScript, bool> filter = null)
        {
            if (filter == null) filter = ScriptSelector;
            Util.PrepareExportDirectory(exportPath);
            var scriptExporter = new ScriptExporter(GameDir, exportPath);
            scriptExporter.DoExport(filter);
        }
       
        static bool ScriptSelector(uTinyRipper.Classes.Object asset)
        {
            if (asset is MonoScript) return true;
            return false;
        }
        private void RequestAssembly(string asm)
        {
            Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Requested Assembly {asm}");
            if (!File.Exists($@"{GameDir}\Managed\{asm}.dll"))
            {
                Logger.Instance.Log(LogType.Warning, LogCategory.Debug, $"Can't find {asm}");
                return;
                //throw new Exception($"Can't find assembly {asm}");
            }
            fileCollection.LoadAssembly($@"{GameDir}\Managed\{asm}.dll");
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
                        fileCollection.Load($@"{GameDir}\{path}\{dep}");
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
        public static void ExportDLL(string GameDir, string dllPath, string exportPath)
        {
            Util.PrepareExportDirectory(exportPath);
            var scriptExporter = new ScriptExporter(GameDir, exportPath);
            scriptExporter.DoExportDLL(dllPath);

        }
        void DoExportDLL(string dllPath)
        {
            fileCollection = new FileCollection(new FileCollection.Parameters()
            {
                RequestAssemblyCallback = RequestAssembly,
                RequestDependencyCallback = RequestDepency,
                RequestResourceCallback = RequestResource
            });
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var assemblyManager = (AssemblyManager)fileCollection.AssemblyManager;
            fileCollection.AssemblyManager.Load(dllPath);
            fileCollection.AssemblyManager.Load(@"C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed\UnityModManager\UnityModManager.dll");
            fileCollection.Exporter.Export(ExportPath, fileCollection, new Object[] { });
            ScriptExportManager scriptManager = new ScriptExportManager(ExportPath);
            AssemblyDefinition myLibrary = AssemblyDefinition.ReadAssembly(dllPath);
            var refrences = myLibrary.MainModule.AssemblyReferences;
            var reference = refrences.First().Name;
            foreach (ModuleDefinition module in myLibrary.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    continue;
                    if (!type.IsClass || type.Name == "<Module>") continue;
                    var libName = myLibrary.Name.Name;
                    var @namespace = type.Namespace;
                    var className = type.Name;
                    var exportType = assemblyManager.CreateExportType(scriptManager, libName, @namespace, className);
                    scriptManager.Export(exportType);
                }
            }
            foreach (TypeDefinition type in myLibrary.MainModule.Types)
            {
                if (!type.IsClass || type.Name == "<Module>") continue;
                var libName = myLibrary.Name.Name;
                var @namespace = type.Namespace;
                var className = type.Name;
                var exportType = assemblyManager.CreateExportType(scriptManager, libName, @namespace, className);
                scriptManager.Export(exportType);
            }
            //fileCollection.AssemblyManager.CreateExportType();
            //CreateExportType(exportManager, AssemblyName, Namespace, ClassName);
            //scriptManager.ExportRest();

        }
        void DoExportAll()
        {
            Config.IsGenerateGUIDByContent = true;
            fileCollection = new FileCollection(new FileCollection.Parameters()
            {
                RequestAssemblyCallback = RequestAssembly,
                RequestDependencyCallback = RequestDepency,
                RequestResourceCallback = RequestResource
            });
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var filePath = Path.Combine(GameDir, "globalgamemanagers.assets");
            fileCollection.Load(filePath);
            var gameAssets = fileCollection.Files.First(f => f is SerializedFile sf && sf.FilePath == filePath);
            var scripts = gameAssets.FetchAssets().Where(o => o is MonoScript ms).ToArray();
            fileCollection.Exporter.Export(ExportPath, fileCollection, scripts);
            ScriptFixer.FixScripts(ExportPath);
        }
        //Refer MonoManager, ScriptAssetExporter, ScriptExportManager
        void DoExport(Func<MonoScript, bool> selector = null)
        {
            fileCollection = new FileCollection(new FileCollection.Parameters()
            {
                RequestAssemblyCallback = RequestAssembly,
                RequestDependencyCallback = RequestDepency,
                RequestResourceCallback = RequestResource
            });
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var filePath = Path.Combine(GameDir, "globalgamemanagers.assets");
            fileCollection.Load(filePath);
            var gameAssets = fileCollection.Files.First(f => f is SerializedFile sf && sf.FilePath == filePath);
            var assets = gameAssets.FetchAssets().Where(o => o is MonoScript ms && selector(ms)).ToArray();

            ScriptExportManager scriptManager = new ScriptExportManager(ExportPath);
            Dictionary<Object, ScriptExportType> exportTypes = new Dictionary<Object, ScriptExportType>();
            foreach (Object asset in assets)
            {
                MonoScript script = (MonoScript)asset;
                ScriptExportType exportType = script.CreateExportType(scriptManager);
                exportTypes.Add(asset, exportType);
            }
            foreach (KeyValuePair<Object, ScriptExportType> exportType in exportTypes)
            {
                string path = scriptManager.Export(exportType.Value);
            }
            //scriptManager.ExportRest();

            //ScriptFixer.FixScripts(ExportPath);

        }
    }
}
