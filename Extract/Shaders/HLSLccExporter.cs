using DXShaderRestorer;
using HLSLccWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper.Classes.Shaders;

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
			/*byte[] exportData = DXShaderProgramRestorer.RestoreProgramData(version, m_graphicApi, subProgram);
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
			}*/
		}
		WrappedGLLang m_GLLang;
		protected readonly GPUPlatform m_graphicApi;
	}
}
