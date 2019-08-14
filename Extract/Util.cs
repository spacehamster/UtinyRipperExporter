using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using uTinyRipper;
using uTinyRipper.Assembly;
using uTinyRipper.Classes;
using uTinyRipper.SerializedFiles;
using YamlDotNet.RepresentationModel;

namespace Extract
{
	public class Util
	{
		public static BindingFlags AllBindingFlags = BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.GetField
			| BindingFlags.SetField
			| BindingFlags.GetProperty
			| BindingFlags.SetProperty;
		public static void PrepareExportDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				DeleteDirectory(path);
			}
		}
		public static void DeleteDirectory(string path)
		{
			foreach (string directory in Directory.GetDirectories(path))
			{
				Thread.Sleep(1);
				DeleteDir(directory);
			}
			DeleteDir(path);
		}

		private static void DeleteDir(string dir)
		{
			try
			{
				Thread.Sleep(1);
				Directory.Delete(dir, true);
			}
			catch (IOException)
			{
				DeleteDir(dir);
			}
			catch (UnauthorizedAccessException)
			{
				DeleteDir(dir);
			}
		}
		public static string GetRelativePath(string filePath, string folder)
		{
			Uri pathUri = new Uri(filePath);
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}
		public static string NormalizePath(string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
					   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					   .ToUpperInvariant();
		}
		public static ISerializedFile FindFile(FileCollection fileCollection, string path)
		{
			return fileCollection.Files.First(f => f is SerializedFile sf && NormalizePath(sf.FilePath) == NormalizePath(path));
		}
		public static void ReplaceInFile(string filePath, string source, string replacement)
		{
			//if (!File.Exists(filePath)) return;
			var text = File.ReadAllText(filePath);
			text = text.Replace(source, replacement);
			File.WriteAllText(filePath, text);
		}
		public static void InsertInFile(string filePath, int index, string replacement)
		{
			//if (!File.Exists(filePath)) return;
			var lines = File.ReadAllLines(filePath).ToList();
			lines.Insert(index, replacement);
			File.WriteAllLines(filePath, lines);
		}
		public static List<string> GetManifestDependencies(string filePath)
		{
			var yaml = new YamlStream();
			YamlMappingNode mapping = null;
			using (var fs = File.OpenText(filePath))
			{
				yaml.Load(fs);
				mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
			}
			var dependencies = (YamlSequenceNode)mapping.Children[new YamlScalarNode("Dependencies")];
			var results = dependencies
				.Select(node => Path.GetFileName(((YamlScalarNode)node).Value))
				.ToList();
			return results;
		}
		internal static List<string> GetManifestAssets(string filePath)
		{
			var lines = File.ReadAllLines(filePath).ToList();
			bool isAtDependencies = false;
			var results = new List<string>();
			foreach (var line in lines)
			{
				if (isAtDependencies)
				{
					if (line.StartsWith("- "))
					{
						var dep = line.Replace("- ", "");
						results.Add(Path.GetFileName(dep));
					}
					else
					{
						break;
					}
				}
				else
				{
					if (line.StartsWith("Assets:")) isAtDependencies = true;
				}
			}
			return results;
		}
		public static string FormatTime(TimeSpan obj)
		{
			StringBuilder sb = new StringBuilder();
			if (obj.Hours != 0)
			{
				sb.Append(obj.Hours);
				sb.Append(" ");
				sb.Append("hours");
				sb.Append(" ");
			}
			if (obj.Minutes != 0 || sb.Length != 0)
			{
				sb.Append(obj.Minutes);
				sb.Append(" ");
				sb.Append("minutes");
				sb.Append(" ");
			}
			if (obj.Seconds != 0 || sb.Length != 0)
			{
				sb.Append(obj.Seconds);
				sb.Append(" ");
				sb.Append("seconds");
				sb.Append(" ");
			}
			if (obj.Milliseconds != 0 || sb.Length != 0)
			{
				sb.Append(obj.Milliseconds);
				sb.Append(" ");
				sb.Append("Milliseconds");
				sb.Append(" ");
			}
			if (sb.Length == 0)
			{
				sb.Append(0);
				sb.Append(" ");
				sb.Append("Milliseconds");
			}
			return sb.ToString();
		}

		public static void FixShaderBundle(FileCollection fileCollection)
		{
			var shaderBundle = fileCollection.Files.FirstOrDefault(f => f is SerializedFile sf && Path.GetFileName(sf.FilePath) == "shaders");
			if (shaderBundle != null)
			{
				foreach (var asset in shaderBundle.FetchAssets())
				{
					if (asset is Shader shader)
					{
						var assetInfoField = typeof(uTinyRipper.Classes.Object).GetField("m_assetInfo", Util.AllBindingFlags);
						var assetInfo = (AssetInfo)assetInfoField.GetValue(asset);
						using (MD5 md5 = MD5.Create())
						{
							byte[] md5Hash = md5.ComputeHash(Encoding.ASCII.GetBytes(shader.ValidName));
							assetInfo.GUID = new EngineGUID(md5Hash);
						}
					}
				}
			}
		}
		public static string GetName(uTinyRipper.Classes.Object asset)
		{
			if(asset is NamedObject no)
			{
				return no.ValidName;
			}
			if(asset is MonoBehaviour mb && mb.IsScriptableObject)
			{
				return mb.Name;
			}
			return "";
		}
		public static void RandomizeAssetGuid(IEnumerable<uTinyRipper.Classes.Object> assets)
		{
			foreach (var asset in assets)
			{
				var assetInfoField = typeof(uTinyRipper.Classes.Object).GetField("m_assetInfo", Util.AllBindingFlags);
				var assetInfo = (AssetInfo)assetInfoField.GetValue(asset);
				assetInfo.GUID = new EngineGUID(Guid.NewGuid());
			}
		}
		public static void SetGUID(uTinyRipper.Classes.Object asset, Guid guid)
		{
			var assetInfoField = typeof(uTinyRipper.Classes.Object).GetField("m_assetInfo", Util.AllBindingFlags);
			var assetInfo = (AssetInfo)assetInfoField.GetValue(asset);
			assetInfo.GUID = new EngineGUID(guid);
		}
	}
}
