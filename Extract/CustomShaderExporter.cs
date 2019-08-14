using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Classes.Shaders.Exporters;
using Shader = uTinyRipper.Classes.Shader;
using Version = uTinyRipper.Version;

namespace Extract
{
	public class CustomShaderExporter
	{
		bool FilterSubPrograms;
		public CustomShaderExporter(bool filterSubPrograms = true)
		{
			FilterSubPrograms = filterSubPrograms;
		}
		public static bool IsSerialized(Version version)
		{
			return version.IsGreaterEqual(5, 5);
		}
		public static bool IsEncoded(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}
		//See Shader.ExportBinary
		public void ExportShader(Shader shader, IExportContainer container, Stream stream,
			Func<Version, GPUPlatform, ShaderTextExporter> exporterInstantiator)
		{
			if (IsSerialized(container.Version))
			{
				using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
				{
					ExportParsedForm(shader.ParsedForm, writer);
				}
			}
			else if (IsEncoded(container.Version))
			{
				using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
				{
					string header = Encoding.UTF8.GetString(shader.Script);
					shader.SubProgramBlob.Export(writer, header);
				}
			}
			else
			{
				var bytes = Encoding.ASCII.GetBytes("/*Default*/\n");
				stream.Write(bytes, 0, bytes.Length);
				shader.ExportBinary(container, stream);
			}
		}
		//See ParsedForm.Export
		void ExportParsedForm(SerializedShader parsedForm, ShaderWriter writer)
		{
			writer.Write("Shader \"{0}\" {{\n", parsedForm.Name);

			parsedForm.PropInfo.Export(writer);

			foreach (SerializedSubShader subShader in parsedForm.SubShaders)
			{
				ExportSerializedSubShader(subShader, writer);
			}

			if (parsedForm.FallbackName != string.Empty)
			{
				writer.WriteIndent(1);
				writer.Write("Fallback \"{0}\"\n", parsedForm.FallbackName);
			}

			if (parsedForm.CustomEditorName != string.Empty)
			{
				writer.WriteIndent(1);
				writer.Write("CustomEditor \"{0}\"\n", parsedForm.CustomEditorName);
			}
			writer.Write('}');
		}
		//See SubShader.Export
		void ExportSerializedSubShader(SerializedSubShader subShader, ShaderWriter writer)
		{
			writer.WriteIndent(1);
			writer.Write("SubShader {\n");
			if (subShader.LOD != 0)
			{
				writer.WriteIndent(2);
				writer.Write("LOD {0}\n", subShader.LOD);
			}
			subShader.Tags.Export(writer, 2);
			foreach (SerializedPass pass in subShader.Passes)
			{
				ExportSerializedPass(pass, writer);
			}
			writer.WriteIndent(1);
			writer.Write("}\n");
		}
		void ExportSerializedPass(SerializedPass pass, ShaderWriter writer)
		{
			writer.WriteIndent(2);
			writer.Write("{0} ", pass.Type.ToString());

			if (pass.Type == SerializedPassType.UsePass)
			{
				writer.Write("\"{0}\"\n", pass.UseName);
			}
			else
			{
				writer.Write("{\n");

				if (pass.Type == SerializedPassType.GrabPass)
				{
					if (pass.TextureName != string.Empty)
					{
						writer.WriteIndent(3);
						writer.Write("\"{0}\"\n", pass.TextureName);
					}
				}
				else if (pass.Type == SerializedPassType.Pass)
				{
					pass.State.Export(writer);

					ExportSerializedProgram(pass.ProgVertex, writer, ShaderType.Vertex);
					ExportSerializedProgram(pass.ProgFragment, writer, ShaderType.Fragment);
					ExportSerializedProgram(pass.ProgGeometry, writer, ShaderType.Geometry);
					ExportSerializedProgram(pass.ProgHull, writer, ShaderType.Hull);
					ExportSerializedProgram(pass.ProgDomain, writer, ShaderType.Domain);

#warning ProgramMask?
#warning HasInstancingVariant?
				}
				else
				{
					throw new NotSupportedException($"Unsupported pass type {pass.Type}");
				}

				writer.WriteIndent(2);
				writer.Write("}\n");
			}
		}
		private static HashSet<ShaderGpuProgramType> GetIsTierLookup(IReadOnlyList<SerializedSubProgram> subPrograms)
		{
			HashSet<ShaderGpuProgramType> lookup = new HashSet<ShaderGpuProgramType>();
			Dictionary<ShaderGpuProgramType, byte> seen = new Dictionary<ShaderGpuProgramType, byte>();
			foreach (SerializedSubProgram subProgram in subPrograms)
			{
				if (seen.ContainsKey(subProgram.GpuProgramType))
				{
					if (seen[subProgram.GpuProgramType] != subProgram.ShaderHardwareTier)
					{
						lookup.Add(subProgram.GpuProgramType);
					}
				}
				else
				{
					seen[subProgram.GpuProgramType] = subProgram.ShaderHardwareTier;
				}
			}
			return lookup;
		}
		void ExportSerializedProgram(SerializedProgram serializedProgram, ShaderWriter writer, ShaderType type)
		{
			if (serializedProgram.SubPrograms.Count > 0)
			{
				writer.WriteIndent(3);
				writer.Write("Program \"{0}\" {{\n", type.ToProgramTypeString());
				HashSet<ShaderGpuProgramType> isTierLookup = GetIsTierLookup(serializedProgram.SubPrograms);
				var subprograms = serializedProgram.SubPrograms;
				if (FilterSubPrograms)
				{
					var best = serializedProgram.SubPrograms
					.OrderByDescending(subProgram =>
					{
						GPUPlatform platform = subProgram.GpuProgramType.ToGPUPlatform(writer.Platform);
						int index = writer.Shader.Platforms.IndexOf(platform);
						ShaderSubProgramBlob blob = writer.Shader.SubProgramBlobs[index];
						var sp = blob.SubPrograms[(int)subProgram.BlobIndex];
						return sp.ProgramData.Count;
										//return sp.GlobalKeywords.Sum(keyword => keyword.Contains("ON") ? 2 : 1);
					}).FirstOrDefault();
					subprograms = new SerializedSubProgram[] { best };
				}
				foreach (SerializedSubProgram subProgram in subprograms)
				{
					Platform uplatform = writer.Platform;
					GPUPlatform platform = subProgram.GpuProgramType.ToGPUPlatform(uplatform);
					int index = writer.Shader.Platforms.IndexOf(platform);
					ShaderSubProgramBlob blob = writer.Shader.SubProgramBlobs[index];
					bool isTier = isTierLookup.Contains(subProgram.GpuProgramType);
					ExportSerializedSubProgram(subProgram, writer, blob, type, isTier);
				}
				writer.WriteIndent(3);
				writer.Write("}\n");
			}
		}

		void ExportSerializedSubProgram(SerializedSubProgram subProgram, ShaderWriter writer, ShaderSubProgramBlob blob, ShaderType type, bool isTier)
		{
			writer.WriteIndent(4);
			writer.Write("SubProgram \"{0} ", subProgram.GpuProgramType.ToGPUPlatform(writer.Platform));
			if (isTier)
			{
				writer.Write("hw_tier{0} ", subProgram.ShaderHardwareTier.ToString("00"));
			}
			writer.Write("\" {\n");
			writer.WriteIndent(5);

			var shaderSubProgram = blob.SubPrograms[(int)subProgram.BlobIndex];
			string hash = Hash(shaderSubProgram.ProgramData.ToArray());
			var filesteam = writer.BaseStream as FileStream;
			var folder = Path.GetDirectoryName(filesteam.Name);

			ExportShaderSubProgram(shaderSubProgram, writer, type);

			writer.Write('\n');
			writer.WriteIndent(4);
			writer.Write("}\n");
		}
		static bool IsReadLocalKeywords(Version version)
		{
			return version.IsGreaterEqual(2019);
		}
		static string Hash(byte[] data)
		{
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				byte[] hashBytes = md5.ComputeHash(data);
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
				return sb.ToString();
			}
		}
		//Refer ShaderSubProgram.Export
		void ExportShaderSubProgram(ShaderSubProgram subProgram, ShaderWriter writer, ShaderType type)
		{
			if (subProgram.GlobalKeywords.Count > 0)
			{
				writer.Write("Keywords { ");
				foreach (string keyword in subProgram.GlobalKeywords)
				{
					writer.Write("\"{0}\" ", keyword);
				}
				if (IsReadLocalKeywords(writer.Version))
				{
					foreach (string keyword in subProgram.LocalKeywords)
					{
						writer.Write("\"{0}\" ", keyword);
					}
				}
				writer.Write("}\n");
				writer.WriteIndent(5);
			}

			writer.Write("\"!!{0}", subProgram.ProgramType.ToShaderName(writer.Platform, type));
			if (subProgram.ProgramData.Count > 0)
			{
				writer.Write("\n");
				writer.WriteIndent(5);
				switch (subProgram.ProgramType.ToGPUPlatform(writer.Platform))
				{
					case GPUPlatform.d3d11:
					case GPUPlatform.d3d11_9x:
					case GPUPlatform.d3d9:
						ExportGLSL(subProgram, writer);
						break;
					default:
						writer.WriteShaderData(
							subProgram.ProgramType.ToGPUPlatform(writer.Platform),
							subProgram.ProgramData.ToArray());
						break;
				}

			}
			writer.Write('"');
		}
		void ExportGLSL(ShaderSubProgram subProgram, ShaderWriter writer)
		{
			string hash = Hash(subProgram.ProgramData.ToArray());

			var filesteam = writer.BaseStream as FileStream;
			var folder = Path.GetDirectoryName(filesteam.Name);
			var name = Path.GetFileNameWithoutExtension(filesteam.Name);
			name = $"{name}_{hash}.glslinc";
			string glslPath = $"{folder}/{name}";
			writer.WriteLine("GLSLPROGRAM");
			writer.WriteIndent(5);
			writer.WriteLine($"#include {name}");
			writer.WriteIndent(5);
			writer.WriteLine("ENDGLSL");
			writer.WriteIndent(5);
			if (!File.Exists(glslPath))
			{
				var data = DXShaderExporter.DXShaderObjectExporter.GetObjectData(writer.Version, subProgram.ProgramType.ToGPUPlatform(writer.Platform), subProgram);

				var ext = new HLSLccWrapper.WrappedGlExtensions();
				ext.ARB_explicit_attrib_location = 1;
				ext.ARB_explicit_uniform_location = 1;
				ext.ARB_shading_language_420pack = 0;
				ext.OVR_multiview = 0;
				ext.EXT_shader_framebuffer_fetch = 0;
				var shader = HLSLccWrapper.Shader.TranslateFromMem(data,
					HLSLccWrapper.WrappedGLLang.LANG_DEFAULT, ext);
				if (shader.OK != 0)
				{
					File.WriteAllText(glslPath, shader.Text);
				}
				else
				{
					writer.WriteLine($"//Error with {name} {shader.OK}");
				}
			}
		}
	}
}
