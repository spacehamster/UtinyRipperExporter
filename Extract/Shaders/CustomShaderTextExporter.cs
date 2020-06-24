using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Converters.Shaders;

namespace Extract
{
	public class CustomShaderTextExporter : ShaderTextExporter
	{
		public virtual string Extension => ".txt";
		public virtual void DoExport(string filePath, uTinyRipper.Version version, ref ShaderSubProgram subProgram)
		{

		}
		static string ProgramTypeToName(ShaderGpuProgramType programType)
		{
			switch (programType)
			{
				case ShaderGpuProgramType.Unknown:
					return "UNK";
				case ShaderGpuProgramType.GLLegacy:
				case ShaderGpuProgramType.GLES31AEP:
				case ShaderGpuProgramType.GLES31:
				case ShaderGpuProgramType.GLES3:
				case ShaderGpuProgramType.GLES:
				case ShaderGpuProgramType.GLCore32:
				case ShaderGpuProgramType.GLCore41:
				case ShaderGpuProgramType.GLCore43:
					return "GL";
				case ShaderGpuProgramType.DX9VertexSM20:
				case ShaderGpuProgramType.DX9VertexSM30:
				case ShaderGpuProgramType.DX10Level9Vertex:
				case ShaderGpuProgramType.DX11VertexSM40:
				case ShaderGpuProgramType.DX11VertexSM50:
				case ShaderGpuProgramType.MetalVS:
					return "VS";
				case ShaderGpuProgramType.DX9PixelSM20:
				case ShaderGpuProgramType.DX9PixelSM30:
				case ShaderGpuProgramType.DX10Level9Pixel:
				case ShaderGpuProgramType.DX11PixelSM40:
				case ShaderGpuProgramType.DX11PixelSM50:
				case ShaderGpuProgramType.MetalFS:
					return "PS";
				case ShaderGpuProgramType.DX11GeometrySM40:
				case ShaderGpuProgramType.DX11GeometrySM50:
					return "GS";
				case ShaderGpuProgramType.DX11HullSM50:
					return "HS";
				case ShaderGpuProgramType.DX11DomainSM50:
					return "DS";
				case ShaderGpuProgramType.Console:
					return "CON";
				default:
					return "UNK";
			}
		}
		public override void Export(ShaderWriter writer, ref ShaderSubProgram subProgram)
		{
			var filePath = (writer.BaseStream as FileStream).Name;
			var outDir = $"{Path.GetDirectoryName(filePath)}/{Path.GetFileNameWithoutExtension(filePath)}";
			Directory.CreateDirectory(outDir);
			var filename = $"{ProgramTypeToName(subProgram.GetProgramType(writer.Version))}_{Util.HashBytes(subProgram.ProgramData)}";
			string outPath = $"{outDir}/{filename}{Extension}";
			string error = null;
			if (!File.Exists(outPath))
			{
				try
				{
					DoExport(outPath, writer.Version, ref subProgram);
				}
				catch (Exception ex)
				{
					if (!File.Exists(outPath))
					{
						error = $"Error exporting shader {filename} - {writer.Shader.ValidName}\n{ex}";
						File.WriteAllText(outPath, error);
						Logger.Log(LogType.Error, LogCategory.Export, error);
					}
				}
			}
			writer.WriteLine($"{filename}{Extension}");
			if (error != null) writer.WriteLine(error);
		}
	}
}
