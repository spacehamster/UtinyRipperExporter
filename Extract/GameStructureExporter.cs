using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Assembly;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.SerializedFiles;
using uTinyRipperGUI.Exporters;
using DateTime = System.DateTime;

namespace Extract
{
    class GameStructureExporter
    {
        string GameDir;
        string AssetPath;
        string ExportPath;
        GameStructure m_GameStructure = null;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        private GameStructureExporter(string gameDir, string exportPath, List<string> files)
        {
            GameDir = gameDir;
            AssetPath = gameDir;
            ExportPath = exportPath;
            var assetPaths = (new List<string>()
            {
                GameDir,
            });
            assetPaths.AddRange(files);
            assetPaths = assetPaths
                .Where(f => !f.ToLower().EndsWith("resources.assets"))
                .ToList();
            m_GameStructure = GameStructure.Load(assetPaths);
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
        public static void ExportGamestructure(string gameDir, string exportPath)
        {
            new GameStructureExporter(gameDir, exportPath, new List<string>()).Export();
        }
        public static void ExportMultiple(string gameDir, IEnumerable<string> assetPaths, string exportPath)
        {
            Util.PrepareExportDirectory(exportPath);
            new GameStructureExporter(gameDir, exportPath, assetPaths.ToList());
        }
    }
}
