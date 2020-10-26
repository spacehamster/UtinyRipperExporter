using DXShaderRestorer;
using HLSLccWrapper;
using System;
using System.IO;
using uTinyRipper.Classes.Shaders;
using uTinyRipperGUI.Exporters;

namespace Extract
{
	public class HLSLccExporter : CustomShaderTextExporter
	{
		public HLSLccExporter(GPUPlatform graphicApi, WrappedGLLang lang)
		{
			m_graphicApi = graphicApi;
			m_GLLang = lang;
		}
		public override string Extension => ".glsl";
		public override void DoExport(string filePath, uTinyRipper.Version version, ref ShaderSubProgram subProgram)
		{
			using (MemoryStream stream = new MemoryStream(subProgram.ProgramData))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					DXDataHeader header = new DXDataHeader();
					header.Read(reader, version);
					if(header.UAVs > 0)
					{
						File.WriteAllText(filePath, "Cannot convert HLSL shaders with UAVs to GLSL");
						return;
					}
					byte[] exportData = DXShaderProgramRestorer.RestoreProgramData(reader, version, ref subProgram);
					WrappedGlExtensions ext = new WrappedGlExtensions();
					ext.ARB_explicit_attrib_location = 1;
					ext.ARB_explicit_uniform_location = 1;
					ext.ARB_shading_language_420pack = 0;
					ext.OVR_multiview = 0;
					ext.EXT_shader_framebuffer_fetch = 0;
					Shader shader = Shader.TranslateFromMem(exportData, m_GLLang, ext);
					if (shader.OK == 0)
					{
						throw new Exception($"Error {shader.OK}");
					}
					else
					{
						File.WriteAllText(filePath, shader.Text);
					}
				}
			}
		}
		WrappedGLLang m_GLLang;
		protected readonly GPUPlatform m_graphicApi;
	}
}
