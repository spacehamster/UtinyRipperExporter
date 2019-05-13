using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.BundleFiles;

namespace Extract
{
    class ResourceHandler
    {
        public static string GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data";
        static string ExportDir = @"C:\Files\Export";
        static HashSet<string> missingAssemblies = new HashSet<string>();
        public static void RequestAssembly(FileCollection fileCollection, string asm)
        {
            if (missingAssemblies.Contains(asm)) return;
            var assemblyPath = $"{GameDir}/Managed/{asm}.dll";
            if (File.Exists(assemblyPath))
            {
                Logger.Instance.Log(LogType.Debug, LogCategory.Debug, $"Loading assembly {asm}");
                fileCollection.AssemblyManager.Load(assemblyPath);
            }
            else
            {
                if (asm == "Assembly-CSharp") throw new Exception("Can't find Assembly-CSharp");
                Logger.Instance.Log(LogType.Warning, LogCategory.Debug, $"Can't find assembly {asm}");
                missingAssemblies.Add(asm);
            }
        }
        static Dictionary<string, string> DependencyLookup = new Dictionary<string, string>();
        static HashSet<string> SeenDependencies = new HashSet<string>();
        public static void RequestDependency(FileCollection fileCollection, string AssetPath, string dep)
        {
            Logger.Log(LogType.Debug, LogCategory.Debug, $"Requested Dependency {dep}");
            var searchPaths = new string[]
            {
                "",
                "Resources",
                "StreamingAssets\\Bundles"
            };
            if (DependencyLookup.ContainsKey(dep))
            {
                fileCollection.Load(DependencyLookup[dep]);
                return;
            }
            foreach (var path in searchPaths)
            {
                var depPath = $@"{GameDir}\{path}\{dep}";
                if (File.Exists(depPath))
                {
                    DependencyLookup[dep] = depPath;
                    fileCollection.Load(depPath);
                    return;
                }
            }
            if (File.Exists($"{AssetPath}.manifest"))
            {
                var manifestDependencies = Util.GetManifestDependencies($"{AssetPath}.manifest");
                foreach (var manifestDependency in manifestDependencies)
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
                    return;
                }
            }
            throw new Exception($"Couldn't find dependency {dep}");
        }
        public static string RequestResource(string dep)
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
    }
}
