using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper;
using uTinyRipper.Assembly;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.Exporters.Scripts;
using uTinyRipper.SerializedFiles;
using Object = uTinyRipper.Classes.Object;
using Version = uTinyRipper.Version;

namespace Extract
{
    class ScriptExporter
    {
        string GameDir;
        FileCollection fileCollection;
        private string ExportPath;
        ExportOptions options;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        public ScriptExporter(string gameDir, string exportPath)
        {
            GameDir = gameDir;
            ExportPath = exportPath;
            options = new ExportOptions()
            {
                Version = new Version(2017, 3, 0, VersionType.Final, 3),
                Platform = Platform.NoTarget,
                Flags = TransferInstructionFlags.NoTransferInstructionFlags,
            };
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
       
        static bool ScriptSelector(Object asset)
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
                RequestResourceCallback = RequestResource
            });
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var assemblyManager = (AssemblyManager)fileCollection.AssemblyManager;
            fileCollection.AssemblyManager.Load(dllPath);
            fileCollection.Exporter.Export(ExportPath, fileCollection, new Object[] { }, options);
            ScriptExportManager scriptManager = new ScriptExportManager(ExportPath);
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
        void DoExportAll()
        {
            Util.PrepareExportDirectory(ExportPath);
            var managedPath = Path.Combine(GameDir, "Managed");
            var globalgamemanagersPath = Path.Combine(GameDir, "globalgamemanagers.assets");
            var gameStructure = GameStructure.Load(new string[]
            {
                globalgamemanagersPath,
                managedPath
            });
            fileCollection = gameStructure.FileCollection;
            fileCollection.AssemblyManager.ScriptingBackEnd = ScriptingBackEnd.Mono;
            var scripts = fileCollection.FetchAssets().Where(o => o is MonoScript ms).ToArray();
            fileCollection.Exporter.Export(ExportPath, fileCollection, scripts, options);
            ScriptFixer.FixScripts(ExportPath);
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
            ScriptExportManager scriptManager = new ScriptExportManager(ExportPath);
            Dictionary<Object, ScriptExportType> exportTypes = new Dictionary<Object, ScriptExportType>();
            foreach (Object asset in assets)
            {
                MonoScript script = (MonoScript)asset;
                ScriptExportType exportType = script.GetExportType(scriptManager);
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
