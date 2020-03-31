using DotNetDxc;
using DXShaderRestorer;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using uTinyRipper.Classes.Shaders;

namespace Extract
{
	public class AsmExporter : CustomShaderTextExporter
	{
		MethodInfo dissassembleMethod;
		public AsmExporter(GPUPlatform graphicApi)
		{
			m_graphicApi = graphicApi;
			var type = Type.GetType("D3DCompiler.D3DCompiler, uTinyRipperUtility");
			dissassembleMethod = type.GetMethod("D3DDisassemble");
		}
		public override string Extension => ".asm";
		public override void DoExport(string filePath, uTinyRipper.Version version, ref ShaderSubProgram subProgram)
		{
			byte[] exportData = subProgram.ProgramData;
			int dataOffset = DXShaderProgramRestorer.GetDataOffset(version, m_graphicApi, subProgram);
			int dataLength = exportData.Length - dataOffset;
			IntPtr unmanagedPointer = Marshal.AllocHGlobal(dataLength);
			Marshal.Copy(exportData, dataOffset, unmanagedPointer, dataLength);

			var parameters = new object[] { unmanagedPointer, (uint)dataLength, (uint)0, null, null };
			dissassembleMethod.Invoke(null, parameters);
			IDxcBlob disassembly = (IDxcBlob)parameters[4];
			string disassemblyText = GetStringFromBlob(disassembly);
			File.WriteAllText(filePath, disassemblyText);
			Marshal.FreeHGlobal(unmanagedPointer);
		}
		private string GetStringFromBlob(IDxcBlob blob)
		{
			return Marshal.PtrToStringAnsi(blob.GetBufferPointer());
		}
		protected readonly GPUPlatform m_graphicApi;
	}
}
