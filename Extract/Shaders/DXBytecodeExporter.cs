using DXShaderRestorer;
using HLSLccWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper.Classes.Shaders;
using uTinyRipperGUI.Exporters;

namespace Extract
{
	public class DxBytecodeExporter : CustomShaderTextExporter
	{
		public DxBytecodeExporter(GPUPlatform graphicApi, bool restore)
		{
			m_graphicApi = graphicApi;
			m_Restore = restore;
		}
		public override string Extension => ".bin";
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
					byte[] exportData;
					if (m_Restore)
					{
						exportData = DXShaderProgramRestorer.RestoreProgramData(reader, version, ref subProgram);
					} else
					{
						exportData = reader.ReadBytes((int)reader.BaseStream.Length - (int)reader.BaseStream.Position);
					}
					File.WriteAllBytes(filePath, exportData);
				}
			}
		}
		bool m_Restore;
		protected readonly GPUPlatform m_graphicApi;
	}
}
