using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
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
        GameStructure m_GameStructure = null;
        HashSet<string> m_LoadedFiles = new HashSet<string>();
        Func<uTinyRipper.Classes.Object, bool> Filter;
        private GameStructureExporter(ExportSettings settings, List<string> files, Func<uTinyRipper.Classes.Object, bool> filter = null)
        {
            GameDir = settings.GameDir;
            ExportPath = settings.ExportDir;
            options = new ExportOptions()
            {
                Version = new Version(2017, 3, 0, VersionType.Final, 3),
                Platform = Platform.NoTarget,
                Flags = TransferInstructionFlags.NoTransferInstructionFlags,
            };
            m_GameStructure = GameStructure.Load(files);
            if (filter != null)
            {
                Filter = filter;
            }
            else
            {
                Filter = (obj) => true;
            }
            var fileCollection = m_GameStructure.FileCollection;
            var textureExporter = new TextureAssetExporter();
            var engineExporter = new EngineAssetExporter();
            fileCollection.Exporter.OverrideExporter(ClassIDType.Material, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Mesh, engineExporter);
            fileCollection.Exporter.OverrideExporter(ClassIDType.Shader, new DummyShaderExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.TextAsset, new TextAssetExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.AudioClip, new AudioAssetExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.Font, new FontAssetExporter());
            fileCollection.Exporter.OverrideExporter(ClassIDType.MovieTexture, new MovieTextureAssetExporter());
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
                            var newGuid = new Guid(data);
                            Util.SetGUID(script, newGuid);
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
                            var newGuid = new Guid(data);
                            Util.SetGUID(shader, newGuid);
                        }
                    }
                }
            }
        }
        private void Export()
        {
            Util.PrepareExportDirectory(ExportPath);
            m_GameStructure.Export(ExportPath, Filter);
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
        public static void ExportGameStructure(ExportSettings settings, Func<uTinyRipper.Classes.Object, bool> filter = null)
        {
            new GameStructureExporter(settings, new List<string>() { settings.GameDir }, filter).Export();
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
            new GameStructureExporter(settings, toExportList, filter).Export();
        }
    }
}
