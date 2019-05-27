using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipperGUI.Exporters;
using DateTime = System.DateTime;
using Version = uTinyRipper.Version;

namespace Extract
{
    class GameStructureExporter
    {
        string GameDir;
        string AssetPath;
        string ExportPath;
        ExportOptions options;
        GameStructure m_GameStructure = null;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        private GameStructureExporter(string gameDir, string exportPath, List<string> files)
        {
            GameDir = gameDir;
            AssetPath = gameDir;
            ExportPath = exportPath;
            options = new ExportOptions()
            {
                Version = new Version(2017, 3, 0, VersionType.Final, 3),
                Platform = Platform.NoTarget,
                Flags = TransferInstructionFlags.NoTransferInstructionFlags,
            };
            m_GameStructure = GameStructure.Load(files);
            var fileCollection = m_GameStructure.FileCollection;
            var textureExporter = new TextureAssetExporter();
            var engineExporter = new EngineAssetExporter();
            fileCollection.Exporter.OverrideExporter(ClassIDType.Material, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Mesh, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Shader, new DummyShaderExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.Font, engineExporter);
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
        }
        private void Export()
        {
            Util.PrepareExportDirectory(ExportPath);
            m_GameStructure.Export(ExportPath, (asset) => true);
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
        public static void ExportGameStructure(string gameDir, string exportPath)
        {
            new GameStructureExporter(gameDir, exportPath, new List<string>() { gameDir }).Export();
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
        public static void ExportBundles(string gameDir, IEnumerable<string> assetPaths, string exportPath, bool loadBundleDependencies = true)
        {
            Util.PrepareExportDirectory(exportPath);
            var paths = assetPaths.ToList();
            paths.Add($"{gameDir}/Managed");
            var toExportHashSet = new HashSet<string>(assetPaths.Select(p => Util.NormalizePath(p)));
            if (loadBundleDependencies) {
                var queue = new Queue<string>(paths);
                while(queue.Count > 0)
                {
                    var currentAssetPath = queue.Dequeue();
                    currentAssetPath = Util.NormalizePath(currentAssetPath);
                    toExportHashSet.Add(currentAssetPath);
                    if (File.Exists($"{currentAssetPath}.manifest"))
                    {
                        var toCheckList = Util.
                            GetManifestDependencies($"{currentAssetPath}.manifest")
                            .Select(name => ResolveBundleDepndency(gameDir, $"{currentAssetPath}.manifest", name));
                        foreach(var toCheckPath in toCheckList)
                        {
                            if (!toExportHashSet.Contains(toCheckPath)){
                                queue.Enqueue(toCheckPath);
                            }
                        }
                    }
                }
            }
            var toExportList = toExportHashSet.ToList();
            Console.WriteLine($"Exporting Files:\n{string.Join("\n", toExportList)}");
            new GameStructureExporter(gameDir, exportPath, toExportList).Export();
        }
    }
}
