using DotNetDxc;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using uTinyRipper.Classes.Shaders;
using uTinyRipperGUI.Exporters;

namespace Extract
{
	public class AsmExporter : CustomShaderTextExporter
	{
		private static MethodInfo m_DissassembleMethod;
		public static MethodInfo DissassembleMethod
		{
			get
			{
				if(m_DissassembleMethod == null)
				{
					var type = Type.GetType("D3DCompiler.D3DCompiler, uTinyRipperUtility");
					m_DissassembleMethod = type.GetMethod("D3DDisassemble");
				}
				return m_DissassembleMethod;
			}
		}
		public AsmExporter(GPUPlatform graphicApi)
		{
			m_graphicApi = graphicApi;
		}
		public override string Extension => ".asm";
		public static string Disassemble(byte[] exportData, uTinyRipper.Version version, GPUPlatform m_graphicApi)
		{
			int dataOffset = 0;
			if (DXDataHeader.HasHeader(m_graphicApi))
			{
				dataOffset = DXDataHeader.GetDataOffset(version, m_graphicApi);
				uint fourCC = BitConverter.ToUInt32(exportData, dataOffset);
				if (fourCC != DXBCFourCC)
				{
					throw new Exception("Magic number doesn't match");
				}
			}

			int dataLength = exportData.Length - dataOffset;
			IntPtr unmanagedPointer = Marshal.AllocHGlobal(dataLength);
			Marshal.Copy(exportData, dataOffset, unmanagedPointer, dataLength);

			var parameters = new object[] { unmanagedPointer, (uint)dataLength, (uint)0, null, null };
			DissassembleMethod.Invoke(null, parameters);
			IDxcBlob disassembly = (IDxcBlob)parameters[4];
			string disassemblyText = GetStringFromBlob(disassembly);
			Marshal.FreeHGlobal(unmanagedPointer);
			return disassemblyText;
		}
		public override void DoExport(string filePath, uTinyRipper.Version version, ref ShaderSubProgram subProgram)
		{
			byte[] exportData = subProgram.ProgramData;
			string disassemblyText = Disassemble(exportData, version, m_graphicApi);
			File.WriteAllText(filePath, disassemblyText);
		}
		private static string GetStringFromBlob(IDxcBlob blob)
		{
			return Marshal.PtrToStringAnsi(blob.GetBufferPointer());
		}
		/// <summary>
		/// 'DXBC' ascii
		/// </summary>
		protected const uint DXBCFourCC = 0x43425844;

		protected readonly GPUPlatform m_graphicApi;
	}
}
